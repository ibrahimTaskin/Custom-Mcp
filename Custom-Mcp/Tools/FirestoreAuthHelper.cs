using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore.V1;
using Grpc.Auth;
using System.ComponentModel;

namespace Custom_Mcp.Tools;

public static class FirestoreAuthHelper
{
    /// <summary>
    /// Service Account JSON dosyasından kimlik bilgilerini yükler
    /// </summary>
    public static GoogleCredential LoadServiceAccountCredential(string serviceAccountPath)
    {
        if (!File.Exists(serviceAccountPath))
        {
            throw new FileNotFoundException($"Service Account JSON dosyası bulunamadı: {serviceAccountPath}");
        }

        return GoogleCredential.FromFile(serviceAccountPath);
    }

    /// <summary>
    /// Environment variable'dan kimlik bilgilerini kontrol eder
    /// </summary>
    public static bool CheckEnvironmentCredentials()
    {
        var credentialsPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
        
        // Environment variable kontrol et
        if (!string.IsNullOrEmpty(credentialsPath) && File.Exists(credentialsPath))
        {
            return true;
        }
        
        // Credentials klasörü altındaki dosyaları kontrol et
        var defaultPath = Path.Combine("credentials", "serviceAccount.json");
        if (File.Exists(defaultPath))
        {
            return true;
        }
        
        var devPath = Path.Combine("credentials", "serviceAccount-dev.json");
        if (File.Exists(devPath))
        {
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// Kimlik doğrulama yöntemlerini listeler
    /// </summary>
    public static string GetAuthenticationMethods()
    {
        var methods = new List<string>();
        
        if (CheckEnvironmentCredentials())
        {
            var credentialsPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
            if (!string.IsNullOrEmpty(credentialsPath) && File.Exists(credentialsPath))
            {
                methods.Add($"✓ GOOGLE_APPLICATION_CREDENTIALS: {credentialsPath}");
            }
            else
            {
                // Credentials klasörü altındaki dosyaları kontrol et
                var defaultPath = Path.Combine("credentials", "serviceAccount.json");
                var devPath = Path.Combine("credentials", "serviceAccount.json");
                
                if (File.Exists(defaultPath))
                {
                    methods.Add($"✓ Credentials klasörü: {defaultPath}");
                }
                else if (File.Exists(devPath))
                {
                    methods.Add($"✓ Development credentials: {devPath}");
                }
            }
        }
        else
        {
            methods.Add("✗ Hiçbir kimlik dosyası bulunamadı");
            methods.Add("  - credentials/serviceAccount.json");
            methods.Add("  - credentials/serviceAccount.json");
            methods.Add("  - GOOGLE_APPLICATION_CREDENTIALS environment variable");
        }

        // Google Cloud SDK kontrolü
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "gcloud",
                    Arguments = "auth list",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            
            if (process.ExitCode == 0 && output.Contains("ACTIVE"))
            {
                methods.Add("✓ Google Cloud SDK ile giriş yapılmış");
            }
            else
            {
                methods.Add("✗ Google Cloud SDK ile giriş yapılmamış");
            }
        }
        catch
        {
            methods.Add("✗ Google Cloud SDK bulunamadı");
        }

        return string.Join("\n", methods);
    }

    /// <summary>
    /// Kimlik doğrulama kurulum talimatlarını döndürür
    /// </summary>
    public static string GetSetupInstructions()
    {
        return @"Firestore Kimlik Doğrulama Kurulum Talimatları:

1. SERVICE ACCOUNT JSON DOSYASI (Önerilen):
   - Google Cloud Console'da Service Account oluşturun
   - JSON anahtar dosyasını indirin
   - AuthenticateFirestore tool'unda serviceAccountPath parametresini kullanın

2. ENVIRONMENT VARIABLE:
   - GOOGLE_APPLICATION_CREDENTIALS environment variable'ını ayarlayın
   - Windows: set GOOGLE_APPLICATION_CREDENTIALS=C:\path\to\serviceAccount.json
   - Linux/Mac: export GOOGLE_APPLICATION_CREDENTIALS=/path/to/serviceAccount.json

3. GOOGLE CLOUD SDK:
   - gcloud auth application-default login komutunu çalıştırın
   - Bu yöntem geliştirme ortamı için uygundur

4. GÜVENLİK NOTLARI:
   - Service Account JSON dosyalarını güvenli tutun
   - Production ortamında environment variable kullanın
   - Gerekli minimum izinleri verin (Firestore User)";
    }
} 