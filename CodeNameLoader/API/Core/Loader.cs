using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using CodeNameLoader.API.Requests;
using CodeNameLoader.API.Responses;
using CodeNameLoader.API.Security;

using Microsoft.Win32;
using Newtonsoft.Json;

using static CodeNameLoader.API.Core.NativeMethods;

namespace CodeNameLoader.API.Core;

internal static class Loader
{
    private static CancellationTokenSource? _requestCTS = null;
    private static Dictionary<string, byte[]> loadedCheats = new();

    private static readonly HttpClient _httpClient = new HttpClient();

    public static async Task<Response<TResponse>?> 
        SendRequest<TResponse, TRequest>
        (Request<TRequest> request,
        string apiEndPoint,
        CancellationToken cancellationToken = default)
    {
        EncryptedResponse? encryptedResponse = null;
        Response<TResponse>? response = null;

        try
        {
            EncryptedRequest? encryptedRequest = null;

            if (request.EncryptedData is null)
                encryptedRequest = new EncryptedRequest()
                {
                    EncryptedData = (await request.EncyrptAsync(cancellationToken)
                        .ConfigureAwait(false)).EncryptedData ??
                        throw new NullReferenceException("Failed to encrypt the request!")
                };

            else
                encryptedRequest = new EncryptedRequest()
                {
                    EncryptedData = request.EncryptedData
                };

            if (encryptedRequest is null)
                throw new NullReferenceException("Failed to create encrypted request!");

            HttpResponseMessage httpResponse =
                await _httpClient
                .PostAsJsonAsync(
                    string.Concat("https://localhost:7179/", apiEndPoint),
                    encryptedRequest!,
                    cancellationToken)
                .ConfigureAwait(false);

            string content = await httpResponse
                .Content
                .ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);

            encryptedResponse = JsonConvert.DeserializeObject<EncryptedResponse>(content);

            if (encryptedResponse is null)
                throw new NullReferenceException("Failed to decrypt encrypted response!");

            response = await new Response<TResponse>(encryptedResponse.EncryptedData)
                .DecryptAsync(cancellationToken)
                .ConfigureAwait(false);
        }

#if DEBUG
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
#endif
        return response;
    }

#if DEBUG
    public static void Debug_LoadDllByFile()
    {
        try
        {
            var openFileDialog = new OpenFileDialog();

            openFileDialog.InitialDirectory = @"c:\";
            openFileDialog.Filter = "dll files (*.dll)|*.dll";
            openFileDialog.FileName = "cheat.dll";

            if (!openFileDialog.ShowDialog() ?? false)
                throw new Exception("Filed to get file path!");

            NativeMethods.Debug_LoadCheat(Process.GetCurrentProcess().ProcessName, openFileDialog.FileName);
        }

        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private unsafe void LoadCheat(byte[] cheatArray)
    {
        try
        {
            var binary_stream = new MemoryStream(cheatArray);

            if (binary_stream is null)
                throw new NullReferenceException("Failed to create binary stream.");

            PEHeaders PEHeaders = new PEHeaders(binary_stream);
            PEHeader? PEHeader = PEHeaders.PEHeader;

            if (PEHeader is null)
                throw new NullReferenceException("No PEHeaders found.");

            if (PEHeaders.SectionHeaders.Length == 0)
                throw new Exception("Length of section headers is 0!");

            IntPtr allocated_memory = NativeMethods.VirtualAlloc
                (
                    0, 
                    (int)binary_stream.Length * 2, 
                    MEM_COMMIT | MEM_RESERVE, 
                    PAGE_EXECUTE_READWRITE
                );


            SectionHeader first_section = PEHeaders.SectionHeaders[0];

            Console.WriteLine($"Memory allocated at {allocated_memory:X16}");
            Console.WriteLine($"{PEHeaders.SectionHeaders.Length} sections");

            Marshal.Copy
            (
                cheatArray,
                0,
                allocated_memory,
                first_section.PointerToRawData
            );

            foreach (var section in PEHeaders.SectionHeaders)
            {
                IntPtr section_allocation = NativeMethods.VirtualAlloc
                    (
                        allocated_memory + section.VirtualAddress, 
                        section.SizeOfRawData, 
                        NativeMethods.MEM_COMMIT, 
                        PAGE_EXECUTE_READWRITE
                    );

                Marshal.Copy
                (
                    cheatArray,
                    section.PointerToRawData,
                    (IntPtr)section_allocation,
                    section.SizeOfRawData
                );

                Console.WriteLine($"Section: {section.Name} - {section.VirtualSize:X8} - {section.VirtualAddress:X8} - {section.SizeOfRawData:X8} {section_allocation:X16}");
            }

            IntPtr import_descriptor = allocated_memory + PEHeader.ImportTableDirectory.RelativeVirtualAddress;

            while (Marshal.ReadInt32(import_descriptor, 0) != 0)
            {
                IntPtr import_table = allocated_memory + Marshal.ReadInt32(import_descriptor, 0);
                IntPtr import_addresses = allocated_memory + Marshal.ReadInt32(import_descriptor, 0x10);
                string? import_module = Marshal.PtrToStringUTF8(allocated_memory + Marshal.ReadInt32(import_descriptor, 0xC));

                if (import_module is null)
                {
                    Console.WriteLine($"Failed to get import module for import adderess: {import_addresses:X16}");
                    continue;
                }

                var import_processes = Process.GetProcessesByName(import_module);
                IntPtr import_handle = IntPtr.Zero;

                if (import_processes.Length == 0)
                    import_handle = NativeLibrary.Load(import_module);
                else
                    import_handle = import_processes[0].Handle;

                while (Marshal.ReadInt32(import_table, 0) != 0)
                {
                    Marshal.WriteInt64
                    (
                        import_addresses,
                        GetProcAddress // I really wish there was a native C# way to do this.
                        (
                            import_handle,
                            Marshal.PtrToStringUTF8(Marshal.ReadInt32(import_table, 0) + allocated_memory + 2) ?? string.Empty
                        )
                    );

                    import_table += 8;
                    import_addresses += 8;
                }

                import_descriptor = import_descriptor + 20;
            }

            IntPtr tls = PEHeader.ThreadLocalStorageTableDirectory.RelativeVirtualAddress + allocated_memory;

            Console.WriteLine("Imports fixed!");
            Console.WriteLine($"{tls:X16}");

            Marshal.WriteInt64(tls + 0, (Marshal.ReadInt64(tls + 0) + allocated_memory - (IntPtr)PEHeader.ImageBase));
            Marshal.WriteInt64(tls + 8, (Marshal.ReadInt64(tls + 8) + allocated_memory - (IntPtr)PEHeader.ImageBase));
            Marshal.WriteInt64(tls + 16, (Marshal.ReadInt64(tls + 16) + allocated_memory - (IntPtr)PEHeader.ImageBase));
            Marshal.WriteInt64(tls + 24, (Marshal.ReadInt64(tls + 24) + allocated_memory - (IntPtr)PEHeader.ImageBase));

            IntPtr load_config = PEHeader.LoadConfigTableDirectory.RelativeVirtualAddress + allocated_memory;

            Console.WriteLine($"{load_config:X16}");
            Marshal.WriteInt64(load_config, 0x58, (Marshal.ReadInt64(load_config, 0x58) + allocated_memory - (IntPtr)PEHeader.ImageBase)); // SecurityCookie
            Marshal.WriteInt64(load_config, 0x70, (Marshal.ReadInt64(load_config, 0x70) + allocated_memory - (IntPtr)PEHeader.ImageBase)); // GuardCFCheckFunctionPointer
            Marshal.WriteInt64(load_config, 0x78, (Marshal.ReadInt64(load_config, 0x78) + allocated_memory - (IntPtr)PEHeader.ImageBase)); // GuardCFDispatchFunctionPointer
            Marshal.WriteInt64(load_config, 0x100, (Marshal.ReadInt64(load_config, 0x100) + allocated_memory - (IntPtr)PEHeader.ImageBase)); // VolatileMetadataPointer
            Marshal.WriteInt64(load_config, 0x118, (Marshal.ReadInt64(load_config, 0x118) + allocated_memory - (IntPtr)PEHeader.ImageBase)); // GuardXFGCheckFunctionPointer
            Marshal.WriteInt64(load_config, 0x120, (Marshal.ReadInt64(load_config, 0x120) + allocated_memory - (IntPtr)PEHeader.ImageBase)); // GuardXFGDispatchFunctionPointer
            Marshal.WriteInt64(load_config, 0x128, (Marshal.ReadInt64(load_config, 0x128) + allocated_memory - (IntPtr)PEHeader.ImageBase)); // GuardXFGTableDispatchFunctionPointer
            Marshal.WriteInt64(load_config, 0x130, (Marshal.ReadInt64(load_config, 0x130) + allocated_memory - (IntPtr)PEHeader.ImageBase)); // CastGuardOsDeterminedFailureMode

            Marshal.WriteInt64(Marshal.ReadIntPtr(load_config, 0x58), (Marshal.ReadInt64(Marshal.ReadIntPtr(load_config, 0x58)) + allocated_memory - (IntPtr)PEHeader.ImageBase));
            Marshal.WriteInt64(Marshal.ReadIntPtr(load_config, 0x70), (Marshal.ReadInt64(Marshal.ReadIntPtr(load_config, 0x70)) + allocated_memory - (IntPtr)PEHeader.ImageBase));
            Marshal.WriteInt64(Marshal.ReadIntPtr(load_config, 0x78), (Marshal.ReadInt64(Marshal.ReadIntPtr(load_config, 0x78)) + allocated_memory - (IntPtr)PEHeader.ImageBase));
            Marshal.WriteInt64(Marshal.ReadIntPtr(load_config, 0x100), (Marshal.ReadInt64(Marshal.ReadIntPtr(load_config, 0x100)) + allocated_memory - (IntPtr)PEHeader.ImageBase));
            Marshal.WriteInt64(Marshal.ReadIntPtr(load_config, 0x118), (Marshal.ReadInt64(Marshal.ReadIntPtr(load_config, 0x118)) + allocated_memory - (IntPtr)PEHeader.ImageBase));
            Marshal.WriteInt64(Marshal.ReadIntPtr(load_config, 0x120), (Marshal.ReadInt64(Marshal.ReadIntPtr(load_config, 0x120)) + allocated_memory - (IntPtr)PEHeader.ImageBase));
            Marshal.WriteInt64(Marshal.ReadIntPtr(load_config, 0x128), (Marshal.ReadInt64(Marshal.ReadIntPtr(load_config, 0x128)) + allocated_memory - (IntPtr)PEHeader.ImageBase));
            Marshal.WriteInt64(Marshal.ReadIntPtr(load_config, 0x130), (Marshal.ReadInt64(Marshal.ReadIntPtr(load_config, 0x130)) + allocated_memory - (IntPtr)PEHeader.ImageBase));

            IntPtr misc_fixes = Marshal.ReadIntPtr(load_config, 0x128) + 8;
            IntPtr tls_callbacks = Marshal.ReadIntPtr(tls, 24);

            while (misc_fixes != tls_callbacks)
            {
                if (Marshal.ReadInt64(misc_fixes) != 0)
                {
                    Marshal.WriteInt64(misc_fixes, Marshal.ReadInt64(misc_fixes) + allocated_memory - (IntPtr)PEHeader.ImageBase);
                }
                misc_fixes = IntPtr.Add(misc_fixes, 8);
            }

            Console.WriteLine("Relocations finished!");

            var DllMain = Marshal.GetDelegateForFunctionPointer<_DllMain>(PEHeader.AddressOfEntryPoint + allocated_memory);
            DllMain(allocated_memory, 1, 0);
        }

        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    public static async Task Debug_DownloadCheatRequest
        (CancellationToken cancellationToken = default)
    {
        try
        {
            var cheatRequest = new CheatRequest()
            {
                Command = CheatCommand.GetCheat,
                UserId = new Guid("08dbc701-be10-4fba-8c58-bf9ade2b8039"),
                UserPassword = "DankBoii",
                GameId = new Guid("a72cd1c1-671c-4aca-9d9b-edee462c1bbc")
            };

            var response = await Loader
                .SendRequest<CheatResponse, CheatRequest>
                (new Request<CheatRequest>(cheatRequest), "Api/Charlie/Input", cancellationToken) ??
                throw new NullReferenceException("Failed to send request!");

            if (response.HasErrors)
            {
                foreach (var errorString in response.Errors ?? Array.Empty<string>())
                    Console.WriteLine(errorString);

                throw new Exception("Server response has errors.");
            }

            if (response.ResponseObject is null)
                throw new NullReferenceException("This response is null!");

            Console.WriteLine("Cheat Response:");
            Console.WriteLine("Cheat Response was successful!");
            Console.WriteLine($"{response.ResponseObject?.GameName}");
            Console.WriteLine($"{response.ResponseObject?.GameVersion}");

            byte[] rawBytes = Convert.FromBase64String(response.ResponseObject!.CheatBinary);

            Debug_LoadDll(
                //response.ResponseObject!.GameProcessName, 
                response.ResponseObject!.CheatBinaryId, 
                rawBytes);
        }

        catch (NullReferenceException ex)
        {
            Console.WriteLine(ex.Message);
        }

        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

    }

    public static async Task Debug_RedeemKeyRequest
        (CancellationToken cancellationToken = default)
    {
        try
        {
            var timeKeyRequest = new TimeKeyRequest()
            {
                Command = TimeKeyCommand.Redeem,
                UserId = new Guid("08dbc701-be10-4fba-8c58-bf9ade2b8039"),
                UserPassword = "DankBoii",
                Key = "NtpRKQpladvazOGN0l0yD0b"
            };

            var response = await Loader
                .SendRequest<TimeKeyResponse, TimeKeyRequest>
                (new Request<TimeKeyRequest>(timeKeyRequest), "Api/Beta/Input", cancellationToken) ??
                throw new NullReferenceException("Failed to send request!");

            if (response.HasErrors)
            {
                foreach (var errorString in response.Errors ?? Array.Empty<string>())
                    Console.WriteLine(errorString);

                throw new Exception("Server response has errors.");
            }

            Console.WriteLine("Time Key Response:");
            Console.WriteLine("Time Key Response was successful!");
        }

        catch (NullReferenceException ex)
        {
            Console.WriteLine(ex.Message);
        }

        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

    }

    public static async Task Debug_RegisterNewUser
        (string userEmail,
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userRequest = new UserRequest()
            {
                UserCommand = UserCommand.Create,
                Email = userEmail,
                Name = username,
                Password = password,
                HardwareId = "DebugHWIDValue"
            };

            var response = await Loader
                .SendRequest<UserResponse, UserRequest>
                (new Request<UserRequest>(userRequest), "Api/Alpha/Input", cancellationToken) ??
                throw new NullReferenceException("Failed to send request!");


            if (response.HasErrors)
            {
                foreach (var errorString in response.Errors ?? Array.Empty<string>())
                    Console.WriteLine(errorString);

                throw new Exception("Server response has errors.");
            }

            Console.WriteLine("Debug Register User Response:");
            Console.WriteLine($"UserName: {response.ResponseObject?.Name}");
            Console.WriteLine($"CreationDate: {response.ResponseObject?.CreationDate}");
            //Console.WriteLine($"RecentIpAddress: {response.ResponseObject?.RecentIp}");
            //Console.WriteLine($"RegistrationIpAddress: {response.ResponseObject?.RegistrationIp}");
            Console.WriteLine($"UserActive: {response.ResponseObject?.Active}");
        }

        catch (NullReferenceException ex)
        {
            Console.WriteLine(ex.Message);
        }

        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

    }

    public static async Task Debug_SendUserAuthRequest
        (CancellationToken cancellationToken = default)
    {
        try
        {
            var userRequest = new UserRequest()
            {
                UserCommand = UserCommand.Authenticate,
                Name = "DiiTz",
                Email = "DebugUser123@mail.com",
                Password = "DankBoii",
                HardwareId = "DebugData"
            };

            var response = await Loader
                .SendRequest<UserResponse, UserRequest>
                (new Request<UserRequest>(userRequest), "Api/Alpha/Input", cancellationToken) ??
                throw new NullReferenceException("Failed to send request!");

            if (response.HasErrors)
            {
                foreach (var errorString in response.Errors ?? Array.Empty<string>())
                    Console.WriteLine(errorString);

                throw new Exception("Server response has errors.");
            }

            Console.WriteLine("User Auth Response:");
            Console.WriteLine($"Name: {response.ResponseObject?.Name}");
            Console.WriteLine($"Email: {response.ResponseObject?.Email}");
            Console.WriteLine($"Admin: {response.ResponseObject?.Admin}");
            Console.WriteLine($"Active: {response.ResponseObject?.Active}");
        }

        catch (NullReferenceException ex)
        {
            Console.WriteLine(ex.Message);
        }

        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

    }
#endif
}
