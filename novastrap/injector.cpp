#define _CRT_SECURE_NO_WARNINGS
#include <windows.h>
#include <tlhelp32.h>
#include <fstream>
#include <string>
#include <map>

using namespace std;

#pragma comment(linker, "/SUBSYSTEM:windows /ENTRY:WinMainCRTStartup")

map<string, string> F;
char P[MAX_PATH] = {0};

string T(const string& s) {
    size_t a = s.find_first_not_of(" \t\r\n");
    if (a == string::npos) return "";
    size_t b = s.find_last_not_of(" \t\r\n");
    return s.substr(a, b - a + 1);
}

void L() {
    ifstream f("flags.txt");
    if (!f.is_open()) return;
    string l;
    while (getline(f, l)) {
        if (l.empty() || l[0] == '#') continue;
        size_t p = l.find('=');
        if (p != string::npos) {
            string k = T(l.substr(0, p));
            string v = T(l.substr(p + 1));
            if (!k.empty()) F[k] = v;
        }
    }
    f.close();
}

void S() {
    ofstream f("C:\\Windows\\Temp\\nova_inject.txt");
    for (auto& x : F) f << x.first << "=" << x.second << "\n";
    f.close();
}

DWORD FR() {
    PROCESSENTRY32 e = { sizeof(PROCESSENTRY32) };
    HANDLE sn = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
    if (sn == INVALID_HANDLE_VALUE) return 0;
    if (Process32First(sn, &e)) {
        do {
            if (_stricmp(e.szExeFile, "RobloxPlayerBeta.exe") == 0 || _stricmp(e.szExeFile, "RobloxPlayer.exe") == 0) {
                CloseHandle(sn);
                return e.th32ProcessID;
            }
        } while (Process32Next(sn, &e));
    }
    CloseHandle(sn);
    return 0;
}

bool ID(DWORD p) {
    HANDLE h = OpenProcess(PROCESS_CREATE_THREAD | PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_VM_READ, FALSE, p);
    if (!h) return false;
    size_t l = strlen(P);
    LPVOID rm = VirtualAllocEx(h, NULL, l + 1, MEM_COMMIT, PAGE_READWRITE);
    if (!rm) { CloseHandle(h); return false; }
    WriteProcessMemory(h, rm, P, l + 1, NULL);
    HMODULE k32 = GetModuleHandleA("kernel32.dll");
    LPTHREAD_START_ROUTINE ll = (LPTHREAD_START_ROUTINE)GetProcAddress(k32, "LoadLibraryA");
    HANDLE ht = CreateRemoteThread(h, NULL, 0, ll, rm, 0, NULL);
    if (!ht) { VirtualFreeEx(h, rm, 0, MEM_RELEASE); CloseHandle(h); return false; }
    WaitForSingleObject(ht, 5000);
    VirtualFreeEx(h, rm, 0, MEM_RELEASE);
    CloseHandle(ht);
    CloseHandle(h);
    return true;
}

bool SR() {
    string pth[] = {
        "C:\\Program Files (x86)\\Roblox\\Versions",
        "C:\\Program Files\\Roblox\\Versions",
        string(getenv("LOCALAPPDATA")) + "\\Roblox\\Versions"
    };
    string lp;
    for (string& b : pth) {
        if (GetFileAttributesA(b.c_str()) == INVALID_FILE_ATTRIBUTES) continue;
        WIN32_FIND_DATAA fd;
        string sp = b + "\\*";
        HANDLE hf = FindFirstFileA(sp.c_str(), &fd);
        if (hf != INVALID_HANDLE_VALUE) {
            do {
                if (fd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY && fd.cFileName[0] != '.') {
                    string vp = b + "\\" + fd.cFileName;
                    string ep = vp + "\\RobloxPlayerBeta.exe";
                    if (GetFileAttributesA(ep.c_str()) != INVALID_FILE_ATTRIBUTES) lp = vp;
                }
            } while (FindNextFileA(hf, &fd));
            FindClose(hf);
        }
    }
    if (lp.empty()) return false;
    string sd = lp + "\\ClientSettings";
    string sf = sd + "\\ClientAppSettings.json";
    CreateDirectoryA(sd.c_str(), NULL);
    ofstream f(sf);
    if (!f.is_open()) return false;
    f << "{\n";
    int i = 0;
    for (auto& x : F) {
        string v = x.second;
        if (v == "true" || v == "false" || v == "True" || v == "False") {
            for (char& c : v) c = tolower(c);
            f << "  \"" << x.first << "\": " << v;
        } else if (isdigit(v[0]) || v[0] == '-') {
            f << "  \"" << x.first << "\": " << v;
        } else {
            f << "  \"" << x.first << "\": \"" << v << "\"";
        }
        if (++i < F.size()) f << ",";
        f << "\n";
    }
    f << "}\n";
    f.close();
    return true;
}

void RS() {
    char sp[] = "C:\\Users\\хз\\Desktop\\novastrap\\obj\\Debug\\net8.0-windows\\ref\\inject.exe";
    STARTUPINFOA si = { sizeof(si) };
    PROCESS_INFORMATION pi;
    si.dwFlags = STARTF_USESHOWWINDOW;
    si.wShowWindow = SW_HIDE;
    CreateProcessA(NULL, sp, NULL, NULL, FALSE, CREATE_NO_WINDOW, NULL, NULL, &si, &pi);
    CloseHandle(pi.hProcess);
    CloseHandle(pi.hThread);
}

int WINAPI WinMain(HINSTANCE a, HINSTANCE b, LPSTR c, int d) {
    ShowWindow(GetConsoleWindow(), SW_HIDE);
    L();
    GetCurrentDirectoryA(260, P);
    strcat(P, "\\payload.dll");
    if (!F.empty()) {
        S();
        SR();
        DWORD p = FR();
        if (p != 0) ID(p);
        DeleteFileA("C:\\Windows\\Temp\\nova_inject.txt");
    }
    RS();
    return 1;
}