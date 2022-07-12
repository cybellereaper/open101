patchinfo
#include "patchinfo.h"
#include "kipacket.h"

#include <QUrl>

PatchInfo::PatchInfo(
    unsigned int latestVersion, unsigned int listFileType,
    unsigned int listFileTime, unsigned int listFileSize,
    unsigned int listFileCRC, QString listFilename,
    QString listFileUrl
) {
    this->latestVersion = latestVersion;
    this->listFileType = listFileType;
    this->listFileTime = listFileTime;
    this->listFileSize = listFileSize;
    this->listFileCRC = listFileCRC;
    this->listFilename = listFilename;
    this->listFileUrl = listFileUrl;
}

unsigned int PatchInfo::getLatestVersion() {
    return this->latestVersion;
}

unsigned int PatchInfo::getListFileType() {
    return this->listFileType;
}

unsigned int PatchInfo::getListFileTime() {
    return this->listFileTime;
}

unsigned int PatchInfo::getListFileSize() {
    return this->listFileSize;
}

unsigned int PatchInfo::getListFileCRC() {
    return this->listFileCRC;
}

QString PatchInfo::getListFilename() {
    return this->listFilename;
}

QString PatchInfo::getListFileUrl() {
    return this->listFileUrl;
}

QUrl PatchInfo::getLatestBuildUrl() {
    return QUrl(listFileUrl).resolved(QUrl("../LatestBuild/"));
}

QString PatchInfo::getVersion() {
    QString version = QUrl(listFileUrl).resolved(QUrl("..")).toString().chopped(1);

    return version.right(version.length() - version.lastIndexOf("/") - 1);
}

PatchInfo *PatchInfo::fromStream(QDataStream &stream) {
    unsigned short innerLength;
    unsigned int latestVersion, listFileType, listFileTime, listFileSize, listFileCRC;

    stream >> innerLength >> latestVersion;
    QString listFilename = KIPacket::readString(stream);
    stream >> listFileType >> listFileTime >> listFileSize >> listFileCRC;
    QString listFileUrl = KIPacket::readString(stream);

    return new PatchInfo(latestVersion, listFileType, listFileTime, listFileSize, listFileCRC, listFilename, listFileUrl);
}