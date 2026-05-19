#include <Windows.h>
#include <TlHelp32.h>
#include <iostream>
#include <fstream>
#include <string>
#include <map>
#include <vector>
#include <algorithm>
#include <psapi.h>

#pragma comment(lib, "psapi.lib")

using namespace std;

map<string, string> g_flags;

string Trim(const string& str) {
    size_t first = str.find_first_not_of(" \t\r\n");
    if (first == string::npos) return "";
    size_t last = str.find_last_not_of(" \t\r\n");
    return str.substr(first, last - first + 1);
}

void LoadFlagsFromFile(const string& path) {
    ifstream file(path);
    if (!file.is_open()) return;
    
    string line;
    while (getline(file, line)) {
        if (line.empty() || line[0] == '#') continue;
        size_t pos = line.find('=');
        if (pos != string::npos) {
            string key = Trim(line.substr(0, pos));
            string val = Trim(line.substr(pos + 1));
            if (!key.empty()) {
                g_flags[key] = val;
            }
        }
    }
    file.close();
}

DWORD FindRobloxProcess() {
    PROCESSENTRY32 entry;
    entry.dwSize = sizeof(PROCESSENTRY32);
    
    HANDLE snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
    if (snapshot == INVALID_HANDLE_VALUE) return 0;
    
    if (Process32First(snapshot, &entry)) {
        do {
            if (_stricmp(entry.szExeFile, "RobloxPlayerBeta.exe") == 0 ||
                _stricmp(entry.szExeFile, "RobloxPlayer.exe") == 0) {
                CloseHandle(snapshot);
                return entry.th32ProcessID;
            }
        } while (Process32Next(snapshot, &entry));
    }
    
    CloseHandle(snapshot);
    return 0;
}

// Получение всех регионов памяти процесса
vector<pair<uintptr_t, uintptr_t>> GetMemoryRegions(HANDLE hProcess) {
    vector<pair<uintptr_t, uintptr_t>> regions;
    SYSTEM_INFO si;
    GetSystemInfo(&si);
    
    uintptr_t addr = (uintptr_t)si.lpMinimumApplicationAddress;
    MEMORY_BASIC_INFORMATION mbi;
    
    while (addr < (uintptr_t)si.lpMaximumApplicationAddress) {
        if (VirtualQueryEx(hProcess, (LPCVOID)addr, &mbi, sizeof(mbi))) {
            if (mbi.State == MEM_COMMIT && 
                (mbi.Type == MEM_PRIVATE || mbi.Type == MEM_IMAGE) &&
                !(mbi.Protect & PAGE_GUARD) &&
                (mbi.Protect & (PAGE_READWRITE | PAGE_EXECUTE_READWRITE | PAGE_READONLY))) {
                regions.push_back({(uintptr_t)mbi.BaseAddress, (uintptr_t)mbi.BaseAddress + mbi.RegionSize});
            }
            addr += mbi.RegionSize;
        } else {
            addr += 0x1000;
        }
    }
    return regions;
}

// Инжект флага с более широким поиском
bool InjectFlag(HANDLE hProcess, const string& key, const string& value, const vector<pair<uintptr_t, uintptr_t>>& regions) {
    for (const auto& region : regions) {
        uintptr_t start = region.first;
        uintptr_t end = region.second;
        uintptr_t size = end - start;
        
        if (size > 10 * 1024 * 1024) size = 10 * 1024 * 1024; // Ограничиваем 10MB за раз
        
        vector<char> buffer(size);
        SIZE_T bytesRead;
        
        if (ReadProcessMemory(hProcess, (LPCVOID)start, buffer.data(), size, &bytesRead)) {
            for (size_t i = 0; i < bytesRead - key.length(); i++) {
                if (memcmp(buffer.data() + i, key.c_str(), key.length()) == 0) {
                    // Пробуем разные смещения для значения
                    for (int offset = key.length(); offset < key.length() + 0x100; offset++) {
                        uintptr_t valueAddr = start + i + offset;
                        SIZE_T written;
                        
                        if (value == "True" || value == "true" || value == "1") {
                            unsigned char val = 1;
                            WriteProcessMemory(hProcess, (LPVOID)valueAddr, &val, 1, &written);
                            return true;
                        } else if (value == "False" || value == "false" || value == "0") {
                            unsigned char val = 0;
                            WriteProcessMemory(hProcess, (LPVOID)valueAddr, &val, 1, &written);
                            return true;
                        } else {
                            int intVal = atoi(value.c_str());
                            WriteProcessMemory(hProcess, (LPVOID)valueAddr, &intVal, 4, &written);
                            return true;
                        }
                    }
                }
            }
        }
    }
    return false;
}

int InjectToRoblox(const string& flagsPath) {
    LoadFlagsFromFile(flagsPath);
    
    if (g_flags.empty()) return -1;
    
    DWORD pid = FindRobloxProcess();
    if (pid == 0) return -2;
    
    HANDLE hProcess = OpenProcess(PROCESS_ALL_ACCESS, FALSE, pid);
    if (hProcess == NULL) {
        // Пробуем с меньшими правами
        hProcess = OpenProcess(PROCESS_VM_READ | PROCESS_VM_WRITE | PROCESS_VM_OPERATION | PROCESS_QUERY_INFORMATION, FALSE, pid);
        if (hProcess == NULL) return -3;
    }
    
    // Получаем все регионы памяти
    auto regions = GetMemoryRegions(hProcess);
    
    int success = 0;
    for (auto& flag : g_flags) {
        if (InjectFlag(hProcess, flag.first, flag.second, regions)) {
            success++;
        }
    }
    
    // Дополнительно ищем FPS лимит
    unsigned char newFPS[4] = { 0xE7, 0x03, 0x00, 0x00 };
    for (const auto& region : regions) {
        uintptr_t start = region.first;
        uintptr_t end = region.second;
        uintptr_t size = end - start;
        if (size > 10 * 1024 * 1024) size = 10 * 1024 * 1024;
        
        vector<char> buffer(size);
        SIZE_T bytesRead;
        
        if (ReadProcessMemory(hProcess, (LPCVOID)start, buffer.data(), size, &bytesRead)) {
            for (size_t i = 0; i < bytesRead - 4; i++) {
                // Ищем значение FPS 60, 120, 144, 240
                if ((buffer[i] == 0x3C && buffer[i+1] == 0x00 && buffer[i+2] == 0x00 && buffer[i+3] == 0x00) ||
                    (buffer[i] == 0x78 && buffer[i+1] == 0x00 && buffer[i+2] == 0x00 && buffer[i+3] == 0x00) ||
                    (buffer[i] == 0x90 && buffer[i+1] == 0x00 && buffer[i+2] == 0x00 && buffer[i+3] == 0x00) ||
                    (buffer[i] == 0xF0 && buffer[i+1] == 0x00 && buffer[i+2] == 0x00 && buffer[i+3] == 0x00)) {
                    SIZE_T written;
                    WriteProcessMemory(hProcess, (LPVOID)(start + i), newFPS, 4, &written);
                }
            }
        }
    }
    
    CloseHandle(hProcess);
    return success;
}

int main(int argc, char* argv[]) {
    string flagsPath = "flags.txt";
    if (argc > 1) {
        flagsPath = argv[1];
    }
    
    // Не скрываем окно для отладки
    // ShowWindow(GetConsoleWindow(), SW_HIDE);
    
    int result = InjectToRoblox(flagsPath);
    
    // Выводим результат в консоль для C#
    if (result > 0) {
        cout << "OK:" << result << endl;
    } else if (result == -1) {
        cout << "ERROR:No flags" << endl;
    } else if (result == -2) {
        cout << "ERROR:Roblox not found" << endl;
    } else if (result == -3) {
        cout << "ERROR:Cannot open process (run as Admin)" << endl;
    }
    
    return result > 0 ? result : 0;
}