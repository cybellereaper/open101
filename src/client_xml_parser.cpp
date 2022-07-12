#ifndef KIXML_H
#define KIXML_H

#include <QByteArray>
#include <QJsonDocument>
#include <QJsonObject>
#include <QVector>

const int STRING_KEY = 0;
const int UINT32_KEY = 1;

struct KeyPair {
    QString key_name;
    int key_type;
};

class KIXML
{
public:
    KIXML();

    unsigned int getElementCount();
    QJsonObject *getRootObj();
    QJsonDocument *getDocument();

    void loadFrom(QByteArray *data);

private:
    unsigned int element_count;
    QJsonObject *root_obj;

    QByteArray *data;
    unsigned int pointer;

    inline quint32 readUint32() {
        pointer += 4;
        return uchar(data->at(pointer-4)) | (uchar(data->at(pointer-3))<<8) | (uchar(data->at(pointer-2))<<16) | (uchar(data->at(pointer-1))<<24);
    }

    inline quint16 readUint16() {
        pointer += 2;
        return uchar(data->at(pointer-2)) | (uchar(data->at(pointer-1))<<8);
    }

    inline quint8 readUint8() {
        pointer += 1;
        return uchar(data->at(pointer-1));
    }

    inline QString readString() {
        quint16 length = readUint16();
        pointer += length;
        return data->mid(pointer - length, length);
    }

    void read_object(QVector<KeyPair> &keys);
};

#endif // KIXML_H
