#include "kixml.h"

#include <QDebug>
#include <QJsonArray>

KIXML::KIXML() : element_count(0), root_obj(new QJsonObject()) {
}

void KIXML::loadFrom(QByteArray *data) {
    this->data = data;
    this->pointer = 0;

    element_count = readUint32();
    pointer += 4; // Unknown

    unsigned int maxLength = data->length();

    QVector<KeyPair> keys;

    QString key_name;
    quint8 key_type_id_1, key_type_id_2;

    while (pointer < maxLength) {
        key_name = readString();
        key_type_id_1 = readUint8();
        key_type_id_2 = readUint8();

        if (key_name != "_TargetTable") {
            int key_type;

            if (key_type_id_1 == 9 && key_type_id_2 == 40) {
                key_type = STRING_KEY;
            } else if (key_type_id_1 == 3 && key_type_id_2 == 40) {
                key_type = UINT32_KEY;
            } else {
                qWarning() << "Unknown key type encountered: " << key_type_id_1 << " " << key_type_id_2;
                continue;
            }

            keys.append({key_name, key_type});
        } else {
            read_object(keys);
            keys.clear();
        }
    }

    data = nullptr;
}

void KIXML::read_object(QVector<KeyPair> &keys) {
    unsigned int maxLength = data->length();

    QString name = readString();
    QJsonArray values;
    quint8 block_type_1, block_type_2;

    while (pointer < maxLength) {
        block_type_1 = readUint8();
        block_type_2 = readUint8();

        if (block_type_1 != 2 || block_type_2 != 2) {
            break;
        }

        pointer += 2; // Unknown
        QJsonObject record;
        QVectorIterator<KeyPair> it(keys);

        while (it.hasNext()) {
            KeyPair key = it.next();
            QJsonValue value;

            switch (key.key_type) {
                case STRING_KEY:
                    value = QJsonValue(readString());
                    break;
                case UINT32_KEY:
                    value = QJsonValue::fromVariant(readUint32());
                    break;
            }

            record.insert(key.key_name, value);
        }

        values.push_back(record);
    }

    pointer += 6; // Unknown, maybe nesting?
    root_obj->insert(name, values);
}

unsigned int KIXML::getElementCount() {
    return element_count;
}

QJsonObject *KIXML::getRootObj() {
    return root_obj;
}

QJsonDocument *KIXML::getDocument() {
    return new QJsonDocument(*root_obj);
}
