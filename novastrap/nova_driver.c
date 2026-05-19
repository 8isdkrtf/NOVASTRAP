// nova_driver.c
#include <ntddk.h>
#include <ntimage.h>

DRIVER_INITIALIZE DriverEntry;

typedef NTSTATUS (*NtCreateFile_t)(
    PHANDLE FileHandle,
    ACCESS_MASK DesiredAccess,
    POBJECT_ATTRIBUTES ObjectAttributes,
    PIO_STATUS_BLOCK IoStatusBlock,
    PLARGE_INTEGER AllocationSize,
    ULONG FileAttributes,
    ULONG ShareAccess,
    ULONG CreateDisposition,
    ULONG CreateOptions,
    PVOID EaBuffer,
    ULONG EaLength
);

NtCreateFile_t original_NtCreateFile = NULL;

NTSTATUS Hooked_NtCreateFile(
    PHANDLE FileHandle,
    ACCESS_MASK DesiredAccess,
    POBJECT_ATTRIBUTES ObjectAttributes,
    PIO_STATUS_BLOCK IoStatusBlock,
    PLARGE_INTEGER AllocationSize,
    ULONG FileAttributes,
    ULONG ShareAccess,
    ULONG CreateDisposition,
    ULONG CreateOptions,
    PVOID EaBuffer,
    ULONG EaLength
) {
    UNICODE_STRING targetPath;
    RtlInitUnicodeString(&targetPath, L"\\??\\C:\\Windows\\Temp\\roblox_flags_temp.json");
    
    if (ObjectAttributes->ObjectName) {
        if (wcsstr(ObjectAttributes->ObjectName->Buffer, L"ClientAppSettings.json")) {
            ObjectAttributes->ObjectName = &targetPath;
            DbgPrint("[NOVASTRAP] Подменил ClientAppSettings.json\n");
        }
    }
    
    return original_NtCreateFile(FileHandle, DesiredAccess, ObjectAttributes,
        IoStatusBlock, AllocationSize, FileAttributes, ShareAccess,
        CreateDisposition, CreateOptions, EaBuffer, EaLength);
}

VOID UnloadDriver(PDRIVER_OBJECT DriverObject) {
    DbgPrint("[NOVASTRAP] Драйвер выгружен\n");
}

NTSTATUS DriverEntry(PDRIVER_OBJECT DriverObject, PUNICODE_STRING RegistryPath) {
    DbgPrint("[NOVASTRAP] Драйвер загружен\n");
    DriverObject->DriverUnload = UnloadDriver;
    return STATUS_SUCCESS;
}