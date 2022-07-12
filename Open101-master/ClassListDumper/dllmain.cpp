// dllmain.cpp : Defines the entry point for the DLL application.
#include "pch.h"
#include <cstdint>
#include <mutex>
#include <vector>
#include <fstream>
#pragma comment(lib, "libMinHook-x86-v141-mdd.lib")

using json = nlohmann::json;

typedef const char* (__fastcall *t_RetString)();

typedef void* (__fastcall *t_PropertyList_Ctor)(void*);
t_PropertyList_Ctor p_PropertyList_Ctor;

struct Type
{
    std::string* GetName()
    {
        return (std::string*)(reinterpret_cast<uint8_t*>(this) + 0x20);
    }

    uint32_t GetHash()
    {
        return *(uint32_t*)(reinterpret_cast<uint8_t*>(this) + 0x38);
    }

    uint32_t GetSize()
    {
        return *(uint32_t*)(reinterpret_cast<uint8_t*>(this) + 0x40);
    }
};

struct Option
{
    uint8_t buf[60];

    std::string* GetStringVal()
    {
        return (std::string*)(reinterpret_cast<uint8_t*>(this) + 0x4);
    }

    std::string* GetName()
    {
        return (std::string*)(reinterpret_cast<uint8_t*>(this) + 0x24);
    }
};

struct PropertyElement
{
    // +40 = PropertyId (some int)
    // +72 = Note (std::string)

    std::string* GetName()
    {
        return (std::string*)(reinterpret_cast<uint8_t*>(this) + 0x2C);
    }

    void* GetContainer()
    {
        return *(void**)(reinterpret_cast<uint8_t*>(this) + 0x20);
    }

    uint32_t GetHash()
    {
        return *(uint32_t*)(reinterpret_cast<uint8_t*>(this) + 0x34);
    }

    uint32_t GetOffset()
    {
        return *(uint32_t*)(reinterpret_cast<uint8_t*>(this) + 0x38);
    }

    Type* GetType()
    {
        return *(Type**)(reinterpret_cast<uint8_t*>(this) + 0x3C);
    }

    uint32_t GetFlags()
    {
        return *(uint32_t*)(reinterpret_cast<uint8_t*>(this) + 0x44);
    }

    std::vector<Option>* GetOptions()
    {
        return (std::vector<Option>*)(reinterpret_cast<uint8_t*>(this) + 0x54);
    }
};

struct PropertyPair
{
    PropertyElement* m_a;
    void* m_b;
};

struct PropertyList
{
    PropertyList* GetParent()
    {
        return *(PropertyList**)(reinterpret_cast<uint8_t*>(this) + 0xC);
    }

    Type* GetType()
    {
        return *(Type**)(reinterpret_cast<uint8_t*>(this) + 0x10);
    }

    std::vector<PropertyPair>* GetProperties()
    {
        return (std::vector<PropertyPair>*)(reinterpret_cast<uint8_t*>(this) + 0x34);
    }
    
    std::string* GetName()
    {
        return (std::string*)(reinterpret_cast<uint8_t*>(this) + 0x74);
    }
};

std::vector<PropertyList*> g_propertyLists;

void* __fastcall Hook_PropertyList_Ctor(void* ptr)
{
    const auto result = p_PropertyList_Ctor(ptr);
    g_propertyLists.push_back((PropertyList*)ptr); 
    return result;
}

struct HashMapNode
{
    HashMapNode* m_nextLower; // 0
    void* m_value; // 4
    HashMapNode* m_nextHigher; // 8
    uint32_t m_key; // 12
    uint32_t m_16; // 16
    uint32_t m_20; // 20
    uint8_t m_24; // 24
    uint8_t m_25; // 25
};

struct HashMap_2
{
    uint32_t m_unk;
    HashMapNode* m_preNode;
};

struct HashMap_1
{
    void* m_vtable;
    HashMap_2 m_2;
};

struct HashMap
{
    HashMap_1* m_1;
    
    // doesn't work
    /*std::vector<std::pair<uint32_t, void*>> ToVector()
    {
        const auto firstRealNode = (HashMapNode*)m_1->m_2.m_preNode->m_value;
        
        std::vector<uint32_t> keys{};        
        CollectKeys(keys, firstRealNode);
        
        std::vector<std::pair<uint32_t, void*>> output{};
        for (auto && key : keys)
        {
            auto valueNode = GetValue(key);
            
            if (valueNode == m_1->m_2.m_preNode || valueNode->m_key > key || valueNode->m_key >= key && 0 < valueNode->m_16)
            {
                valueNode = m_1->m_2.m_preNode;
            } else
            {
                // value node is legit
            }
            
            output.push_back(std::pair<uint32_t, void*>(key, valueNode->m_value));
        }
        
        return output;
    }*/
    
    std::vector<uint32_t> GetKeys()
    {
        const auto firstRealNode = (HashMapNode*)m_1->m_2.m_preNode->m_value;
                
        std::vector<uint32_t> keys{};        
        CollectKeys(keys, firstRealNode);
        return keys;
    }
    
    void CollectKeys(std::vector<uint32_t>& keys, HashMapNode* node)
    {
        if (node->m_25) return; // leaf
        
        CollectKeys(keys, node->m_nextLower);
                
        keys.push_back(node->m_key);
                
        CollectKeys(keys, node->m_nextHigher);
    }
};

struct PropertyClassRTTI
{
    uint32_t m_0;
    std::string m_name;
};

class PropertyClassInstance
{
public:
    virtual PropertyClassRTTI* GetRTTI();
    virtual ~PropertyClassInstance();
    virtual const char* GetClassName();
    virtual PropertyList* GetPropertyList();   
};

struct CoreObjectFactoryThing
{
    virtual void Unk1();
    virtual PropertyClassInstance* Allocate();
};

struct HashMap_Value_CoreObjectFactoryThing
{
    char pad[20];
    CoreObjectFactoryThing* m_factory;
};

DWORD WINAPI ThreadFunc(LPVOID lpThreadParameter)
{        
    while (true)
    {
        Sleep(500);
        
        if (!GetAsyncKeyState(VK_F9)) continue;
        
        const auto pCreateCoreObject = 0xF0E160;
        const auto ppHashMap = pCreateCoreObject + 25;
        
        PropertyClassInstance*(__fastcall *CreateCoreObject)(uint32_t, uint32_t, uint32_t, uint8_t) = (PropertyClassInstance*(__fastcall *)(uint32_t, uint32_t, uint32_t, uint8_t))pCreateCoreObject;
        HashMap* hm = *(HashMap**)ppHashMap;
        
        /*const auto resultA = CreateCoreObject(0, 0, 2, 0)->GetPropertyList()->GetName()->c_str();
        char buf[500];
        sprintf_s(buf, "resultA: %s", resultA);
        MessageBoxA(nullptr, buf, "ClassListDumper", MB_OK);*/
        
        std::ofstream myfile;
        myfile.open("D:\\re\\wiz101\\classes.txt");

        json output = json({});
        json coreTypesDict = json({});
        
        for (uint32_t key : hm->GetKeys())
        {
            PropertyClassInstance* propClass = CreateCoreObject(0, 0, key, 0);
            const auto propertyList = propClass->GetPropertyList();
            coreTypesDict[std::to_string(key)] = propertyList->GetName()->c_str();
        }
        
        DWORD oldProtect;
        VirtualProtect((void*)ppHashMap, 4, PAGE_EXECUTE_READWRITE, &oldProtect);
        *reinterpret_cast<uint32_t*>(ppHashMap) += 0x4; // move hashmap ptr 4 bytes down, where the behavior map is
        hm++; // ptr
        
        json behaviorTypesDict = json({});
        for (uint32_t key : hm->GetKeys())
        {
            PropertyClassInstance* propClass = CreateCoreObject(0, 0, key, 0);
            const auto propertyList = propClass->GetPropertyList();
            behaviorTypesDict[std::to_string(key)] = {
                {"class", propertyList->GetName()->c_str()},
                {"vtable", *reinterpret_cast<uint32_t*>(propClass)},
                {"rtti_name", propClass->GetRTTI()->m_name.c_str()}
            };
        }
        
        json propDict = json({});
        
        for (PropertyList* propertyList : g_propertyLists)
        {
            json classJson = {
                {"hash", propertyList->GetType()->GetHash()},
                {"typeName", propertyList->GetType()->GetName()->c_str()}
            };

            int propertySkipCount = 0;

            const auto parent = propertyList->GetParent();
            if (parent != nullptr)
            {
                classJson["parent"] = parent->GetName()->c_str();
                propertySkipCount = parent->GetProperties()->size();
            }

            json propertyListJson = json::array();
            for (auto && prop : *propertyList->GetProperties())
            {
                if (propertySkipCount > 0)
                {
                    propertySkipCount--;
                    continue;
                }
                
                std::string* propName = prop.m_a->GetName();
                Type* propType = prop.m_a->GetType();

                void* container = prop.m_a->GetContainer();
                void** containerVtable = *(void***)container;
                t_RetString containerGetNameMethod = (t_RetString)containerVtable[1];

                uint32_t propHash = prop.m_a->GetHash();
                uint32_t propOffset = prop.m_a->GetOffset();

                json propertyJson = {
                    {"hash", propHash},
                    {"offset", propOffset},
                    {"size", propType->GetSize()},
                    {"name", propName->c_str()},
                    {"type", propType->GetName()->c_str()},
                    {"container", containerGetNameMethod()},
                    {"flags", prop.m_a->GetFlags()}
                };

                std::vector<Option>* optionVec = prop.m_a->GetOptions();
                if (!optionVec->empty())
                {
                    json optionsJson = json::array();
                    for (auto && option : *optionVec)
                    {
                        optionsJson.push_back({
                            {"name", option.GetName()->c_str()},
                            {"str_val", option.GetStringVal()->c_str()}
                        });
                    }
                    propertyJson["options"] = optionsJson;
                }

                propertyListJson.push_back(propertyJson);
            }
            classJson["properties"] = propertyListJson;
            propDict[propertyList->GetName()->c_str()] = classJson;
        }
        
        output["behavior_types"] = behaviorTypesDict;
        output["core_types"] = coreTypesDict;
        output["classes"] = propDict;
        
        myfile << output.dump(4);
        myfile.close();
        
        break;
    }   
    MessageBoxA(nullptr, "done", "ClassListDumper", MB_OK);
    
    return 0;
}

BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
                     )
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
    {
        if (MH_Initialize() != MH_OK)
        {
            MessageBoxA(nullptr, "MH_Initialize failed", "ClassListDumper", MB_OK);
            return 0;
        }

        uint8_t* mod = (uint8_t*)GetModuleHandleW(nullptr);

        void* ctorPtr = mod + 0x100F940 - 0x400000;

        DWORD oldProtect;
        VirtualProtect(ctorPtr, 100, PAGE_EXECUTE_READWRITE, &oldProtect);
        MH_CreateHook(ctorPtr, &Hook_PropertyList_Ctor, (void**)&p_PropertyList_Ctor);
        MH_EnableHook(ctorPtr);

        DWORD oldProtect2;
        VirtualProtect(ctorPtr, 100, oldProtect, &oldProtect2);
            
        DWORD threadID;
        const auto hThread = CreateThread(NULL, // security attributes ( default if NULL )
            0, // stack SIZE default if 0
            &ThreadFunc, // Start Address
            NULL, // input data
            0, // creational flag ( start if  0 )
            &threadID); // thread ID
        break;
    }
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}

