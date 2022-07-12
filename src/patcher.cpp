#include "patcher.h"
#include "kipacket.h"
#include "patchinfo.h"
#include "globals.h"
#include "kixml.h"
#include "patchworker.h"

#include <QMessageBox>
#include <QDateTime>
#include <QString>
#include <QRandomGenerator>
#include <QtGlobal>

struct EmbeddedEntry {
    QString sourceFilename;
    QString targetFilename;
    bool force;
};

const static QVector<EmbeddedEntry> embedded = {
    {"BugReportBuilder.bin", "Bin/BugReportBuilder.dll", true}
};

Patcher::Patcher(QObject *parent, Logger *logger, Settings *settings, QDir dir) :
    QObject(parent),
    logger(logger),
    settings(settings),
    dir(dir),
    tcpSocket(new QTcpSocket(parent)),
    netMgr(new QNetworkAccessManager(this))
{
    in.setDevice(tcpSocket);
}


Patcher::~Patcher() {
    if (tcpSocket != nullptr) {
        tcpSocket->deleteLater();
    }

    if (netMgr != nullptr) {
        netMgr->deleteLater();
    }

    patchThread.quit();
    patchThread.wait();
};

void Patcher::start() {
    qDebug() << "Connecting to patch server...";
    connect(tcpSocket, &QIODevice::readyRead, this, &Patcher::readyRead);
#if QT_VERSION >= QT_VERSION_CHECK(5, 15, 0)
    connect(tcpSocket, &QAbstractSocket::errorOccurred, this, &Patcher::errorOccurred);
#else
    connect(tcpSocket, QOverload<QAbstractSocket::SocketError>::of(&QAbstractSocket::error), this, &Patcher::errorOccurred);
#endif
    tcpSocket->abort();
    tcpSocket->connectToHost(PATCH_SERVER, 12500, QIODevice::ReadWrite);
}

void Patcher::errorOccurred(QAbstractSocket::SocketError) {
    disconnect(tcpSocket, &QIODevice::readyRead, this, &Patcher::readyRead);
#if QT_VERSION >= QT_VERSION_CHECK(5, 15, 0)
    disconnect(tcpSocket, &QAbstractSocket::errorOccurred, this, &Patcher::errorOccurred);
#else
    disconnect(tcpSocket, QOverload<QAbstractSocket::SocketError>::of(&QAbstractSocket::error), this, &Patcher::errorOccurred);
#endif
    tcpSocket->abort();

    emit patchFailure(QString("Could not contact KingsIsle patch server!\n\n%1").arg(tcpSocket->errorString()));
}

void Patcher::errorOccurredFromThread(QString reason) {
    emit patchFailure(QString("Could not update files!\n\n%1").arg(reason));
}

void Patcher::readyRead() {
    in.startTransaction();
    in.setByteOrder(QDataStream::ByteOrder::LittleEndian);

    unsigned short magicBytes, length, nullBytes;
    unsigned char isOpcode, opcode;

    in >> magicBytes >> length >> isOpcode >> opcode >> nullBytes;

    if (isOpcode == 1 && opcode == 0) {
        qDebug() << "Writing session data...";
        unsigned short sessionId;
        unsigned int undefinedData, milliseconds;
        int timestamp;
        unsigned char blank;

        in >> sessionId >> undefinedData >> timestamp >> milliseconds >> blank;

        if (!in.commitTransaction()) {
            return;
        }

        unsigned int unixTimestamp = QDateTime::currentDateTime().toSecsSinceEpoch();

        KIPacket packet;
        packet.writer << (unsigned char) 0x0d;
        packet.writer << (unsigned char) 0xf0;
        packet.writer << (unsigned char) 0x15;
        packet.writer << (unsigned char) 0x00;
        packet.writer << (unsigned char) 0x01;
        packet.writer << (unsigned char) 0x05;
        packet.writer << (unsigned char) 0x00;
        packet.writer << (unsigned char) 0x00;
        packet.writer << (unsigned short) 0;
        packet.writer << (unsigned int) 0;
        packet.writer << (unsigned int) unixTimestamp;
        packet.writer << (unsigned int) (milliseconds + 100);
        packet.writer << (unsigned short) sessionId;

        packet.writer << (unsigned char) 0;

        KIPacket packet2;
        packet2.writeHeader(0, 0, 8, 1);
        packet2.writer << (unsigned int) 0;
        packet2.writer << (unsigned short) 0;

        for (int i = 0; i < 4; i++) {
            packet2.writer << (unsigned int) 0;
        }

        for (int i = 0; i < 3; i++) {
            packet2.writer << (unsigned short) 0;
        }

        tcpSocket->write(packet.payload);
        tcpSocket->write(packet2.finalize());
        return;
    }

    unsigned char opcode1, opcode2;
    in >> opcode1 >> opcode2;

    if (opcode1 == 8 && opcode2 == 1) {
        patchInfo = PatchInfo::fromStream(in);

        if (!in.commitTransaction()) {
            return;
        }

        qDebug() << "Received latest version.";
        qDebug() << "Patch list available at:" << patchInfo->getListFileUrl();

        disconnect(tcpSocket, &QIODevice::readyRead, this, &Patcher::readyRead);
#if QT_VERSION >= QT_VERSION_CHECK(5, 15, 0)
        disconnect(tcpSocket, &QAbstractSocket::errorOccurred, this, &Patcher::errorOccurred);
#else
        disconnect(tcpSocket, QOverload<QAbstractSocket::SocketError>::of(&QAbstractSocket::error), this, &Patcher::errorOccurred);
#endif
        tcpSocket->abort();

        considerPatch();
        return;
    }

    in.commitTransaction();
}

void Patcher::considerPatch() {
    QString version = patchInfo->getVersion();

    if (settings->isUpdateNecessary(version)) {
        logger->log("Patcher", QString("Current version installed: %1").arg(settings->getCurrentVersion()));
        logger->log("Patcher", QString("Newest version available: %1").arg(version));
        logger->log("Patcher", QString("Patch list available at: %1").arg(patchInfo->getListFileUrl()));
    }

    downloadPatchList();
}

void Patcher::downloadPatchList() {
    QNetworkRequest request(QUrl(patchInfo->getListFileUrl()));
    request.setHeader(QNetworkRequest::ContentTypeHeader, "application/x-www-form-urlencoded");
    request.setHeader(QNetworkRequest::UserAgentHeader, USER_AGENT);

    connect(netMgr, &QNetworkAccessManager::finished, this, &Patcher::patchListDownloaded);
    reply = netMgr->get(request);
}

void Patcher::patchListDownloaded() {
    qDebug() << "Downloading patch list...";

    if (reply->error() != QNetworkReply::NoError) {
        emit patchFailure(QString("Could not download patch list from %1!\n\n%2").arg(reply->url().toString(), reply->errorString()));
        return;
    }

    QByteArray patchList = reply->readAll();
    reply->deleteLater();

    if ((unsigned int) patchList.length() != patchInfo->getListFileSize()) {
        emit patchFailure(QString("Could not download patch list from %1!\nFile size mismatch detected!").arg(reply->url().toString()));
        return;
    }

    qDebug() << "Patch list downloaded.";
    emit beginHashCheck();

    PatchWorker *worker = new PatchWorker(settings);
    worker->moveToThread(&patchThread);
    connect(&patchThread, &QThread::finished, worker, &QObject::deleteLater);
    connect(this, &Patcher::beginPatch, worker, &PatchWorker::beginVerify);
    connect(worker, &PatchWorker::patchFailed, this, &Patcher::errorOccurredFromThread);
    connect(worker, &PatchWorker::patchComplete, this, &Patcher::patchThreadComplete);
    connect(worker, &PatchWorker::patchProgress, this, &Patcher::patchThreadProgress);
    connect(worker, &PatchWorker::fileProgress, this, &Patcher::fileThreadProgress);
    patchThread.start();

    emit beginPatch(patchInfo->getLatestBuildUrl(), patchList, dir.path());
}

void Patcher::patchThreadProgress(int current, int total, QString basename, bool newFile) {
    if (newFile) {
        logger->log("Patcher", QString("Downloading new file: %1").arg(basename));
    } else {
        logger->log("Patcher", QString("Updating file: %1").arg(basename));
    }

    emit patchProgress(current, total, basename, newFile);
}

void Patcher::fileThreadProgress(int percent) {
    emit fileProgress(percent);
}

void Patcher::patchThreadComplete() {
    settings->updateVersion(patchInfo->getVersion());
    settings->save();
    finalizePatch();
}

void Patcher::finalizePatch() {
    if (!writeEmbeddedFiles()) {
        return;
    }

    emit patchComplete();
}

bool Patcher::writeEmbeddedFiles() {
    QByteArray buffer;

    for (const EmbeddedEntry entry : embedded) {
        QFile target(dir.filePath(entry.targetFilename));

        if (target.exists()) {
            if (entry.force) {
                target.remove();
            } else {
                // This file already exists, so we're not going to rewrite it
                continue;
            }
        }

        QFile source(":/xenos/" + entry.sourceFilename);
        source.open(QIODevice::ReadOnly);
        target.open(QIODevice::WriteOnly);

        while(!(buffer = source.read(4096)).isEmpty()){
            target.write(buffer);
        }

        source.close();
        target.close();
    }

    return true;
}