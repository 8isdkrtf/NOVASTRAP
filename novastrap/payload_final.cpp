#include <windows.h>
#include <string>
#include <map>
#include <fstream>

using namespace std;

#define Y0 "C:\\Windows\\Temp\\nova_inject.txt"
#define Y1 "C:\\Windows\\Temp\\roblox_flags_temp.json"

map<string, string> F;
typedef HANDLE(WINAPI* CFW)(LPCWSTR, DWORD, DWORD, LPSECURITY_ATTRIBUTES, DWORD, DWORD, HANDLE);
CFW O = nullptr;

void L() {
    ifstream f(Y0);
    if (!f.is_open()) return;
    string l;
    while (getline(f, l)) {
        size_t p = l.find('=');
        if (p != string::npos) F[l.substr(0, p)] = l.substr(p + 1);
    }
    f.close();
}

HANDLE WINAPI H(LPCWSTR N, DWORD A, DWORD M, LPSECURITY_ATTRIBUTES S, DWORD C, DWORD F_, HANDLE T) {
    wstring WN(N);
    if (WN.find(L"ClientAppSettings.json") != wstring::npos && !F.empty()) {
        ofstream f(Y1);
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
        return O(L"test", A, M, S, C, F_, T);
    }
    return O(N, A, M, S, C, F_, T);
}

void I() {
    HMODULE k = GetModuleHandleW(L"kernel32.dll");
    if (!k) return;
    O = (CFW)GetProcAddress(k, "CreateFileW");
    if (!O) return;
    void* T = (void*)O;
    void* Hc = (void*)H;
    DWORD p;
    VirtualProtect(T, 5, PAGE_EXECUTE_READWRITE, &p);
    uintptr_t o = (uintptr_t)Hc - (uintptr_t)T - 5;
    BYTE j[] = { 0xE9, (BYTE)(o & 0xFF), (BYTE)((o >> 8) & 0xFF), (BYTE)((o >> 16) & 0xFF), (BYTE)((o >> 24) & 0xFF) };
    memcpy(T, j, 5);
    VirtualProtect(T, 5, p, &p);
}

void RS() {
    system("C:\\Users\\хз\\Desktop\\novastrap\\obj\\Debug\\net8.0-windows\\ref\\inject.exe");
}

DWORD WINAPI M(LPVOID) {
    Sleep(5000);
    L();
    I();
    CreateThread(NULL, 0, (LPTHREAD_START_ROUTINE)RS, NULL, 0, NULL);
    return 0;
}

BOOL APIENTRY DllMain(HMODULE h, DWORD r, LPVOID) {
    if (r == DLL_PROCESS_ATTACH) {
        DisableThreadLibraryCalls(h);
        CreateThread(NULL, 0, M, NULL, 0, NULL);
    }
    return TRUE;
}