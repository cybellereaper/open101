#ifndef PATCHER_H
#define PATCHER_H

#include "patchinfo.h"
#include "logger.h"
#include "settings.h"

#include <QObject>
#include <QTcpSocket>
#include <QDataStream>
#include <QNetworkAccessManager>
#include <QNetworkReply>
#include <QPointer>
#include <QDir>
#include <QThread>

class Patcher : public QObject
{
    Q_OBJECT
public:
    explicit Patcher(QObject *parent, Logger *logger, Settings *settings, QDir dir);
    ~Patcher();

private:
    Logger *logger;
    Settings *settings;
    QDir dir;

    QTcpSocket *tcpSocket;
    QDataStream in;

    QPointer<QNetworkAccessManager> netMgr;
    QPointer<QNetworkReply> reply;

    PatchInfo *patchInfo;

    QThread patchThread;

    void considerPatch();
    void downloadPatchList();
    void finalizePatch();

    bool writeXenosConfig();
    bool writeEmbeddedFiles();

public:
    void start();

signals:
    void patchProgress(int current, int total, QString basename, bool newFile);
    void fileProgress(int percent);
    void patchComplete();
    void patchFailure(QString reason);
    void beginHashCheck();

    void beginPatch(QUrl downloadUrl, QByteArray manifestBytes, QString dir);

private slots:
    void readyRead();
    void errorOccurred(QAbstractSocket::SocketError socketError);

    void errorOccurredFromThread(QString reason);
    void patchThreadProgress(int current, int total, QString basename, bool newFile);
    void fileThreadProgress(int percent);
    void patchThreadComplete();

    void patchListDownloaded();
};

#endif // PATCHER_H
