#ifndef PATCHINFO_H
#define PATCHINFO_H

#include <QString>
#include <QDataStream>

class PatchInfo {
public:
    PatchInfo(unsigned int latestVersion, unsigned int listFileType,
              unsigned int listFileTime, unsigned int listFileSize,
              unsigned int listFileCRC, QString listFilename,
              QString listFileUrl);

    unsigned int getLatestVersion();
    unsigned int getListFileType();
    unsigned int getListFileTime();
    unsigned int getListFileSize();
    unsigned int getListFileCRC();
    QString getListFilename();
    QString getListFileUrl();

    QUrl getLatestBuildUrl();
    QString getVersion();

    static PatchInfo *fromStream(QDataStream &stream);
private:
    unsigned int latestVersion, listFileType, listFileTime, listFileSize, listFileCRC;
    QString listFilename;
    QString listFileUrl;

};

#endif // PATCHINFO_H