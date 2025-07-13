using Google.Cloud.Firestore;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore.V1;
using Grpc.Auth;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace Custom_Mcp.Tools;

[McpServerToolType]
public static class FirestoreTools
{
    private static FirestoreDb? _firestoreDb;
    private static string? _currentProjectId;

    [McpServerTool, Description("Firestore veritabanına kimlik doğrulama yapar ve bağlantı kurar.")]
    public static async Task<string> AuthenticateFirestore(
        [Description("Firestore proje ID'si (opsiyonel, .env'den alınır).")] string? projectId = null,
        [Description("Service Account JSON dosyasının tam yolu (opsiyonel, .env'den alınır).")] string? serviceAccountPath = null)
    {
        try
        {
            // Environment variable'lardan değerleri al
            var envProjectId = projectId ?? Environment.GetEnvironmentVariable("FIRESTORE_PROJECT_ID");
            var envServiceAccountPath = serviceAccountPath ?? Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");

            if (string.IsNullOrEmpty(envProjectId))
            {
                return "Firestore proje ID'si bulunamadı. Lütfen .env dosyasında FIRESTORE_PROJECT_ID değerini ayarlayın veya parametre olarak verin.";
            }

            // Credentials klasörü altındaki dosyayı kontrol et
            if (string.IsNullOrEmpty(envServiceAccountPath))
            {
                var defaultServiceAccountPath = Path.Combine("credentials", "serviceAccount.json");
                if (File.Exists(defaultServiceAccountPath))
                {
                    envServiceAccountPath = defaultServiceAccountPath;
                }
            }

            FirestoreDb db;
            
            if (!string.IsNullOrEmpty(envServiceAccountPath))
            {
                // Service Account JSON dosyasından kimlik doğrulama
                var credential = FirestoreAuthHelper.LoadServiceAccountCredential(envServiceAccountPath);
                var clientBuilder = new FirestoreClientBuilder
                {
                    Credential = credential
                };
                var client = await clientBuilder.BuildAsync();
                db = FirestoreDb.Create(envProjectId, client);
            }
            else
            {
                return "Service Account JSON dosyası bulunamadı: credentials/serviceAccount.json";
            }

            // Bağlantıyı test et
            var collections = db.ListRootCollectionsAsync();
            var collectionList = string.Join(", ", collections.Select(c => c.Id));

            _firestoreDb = db;
            _currentProjectId = envProjectId;

            return $"Firestore bağlantısı başarılı!\n" +
                   $"Proje ID: {envProjectId}\n" +
                   $"Mevcut Collection'lar: {collectionList}\n" +
                   $"Kimlik Doğrulama Yöntemi: {(string.IsNullOrEmpty(envServiceAccountPath) ? "Varsayılan Kimlik Bilgileri" : "Service Account JSON")}\n" +
                   $"Service Account: {envServiceAccountPath ?? "Environment Variable kullanılıyor"}";
        }
        catch (Exception ex)
        {
            return $"Firestore kimlik doğrulama hatası: {ex.Message}\n\n" +
                   $"Çözüm önerileri:\n" +
                   $"1. GOOGLE_APPLICATION_CREDENTIALS environment variable'ını ayarlayın\n" +
                   $"2. Service Account JSON dosyasının yolunu belirtin\n" +
                   $"3. Google Cloud SDK ile giriş yapın: gcloud auth application-default login";
        }
    }

    [McpServerTool, Description("Mevcut kimlik doğrulama yöntemlerini kontrol eder ve durum raporu verir.")]
    public static string CheckAuthenticationStatus()
    {
        var status = FirestoreAuthHelper.GetAuthenticationMethods();
        var instructions = FirestoreAuthHelper.GetSetupInstructions();
        
        return $"Kimlik Doğrulama Durumu:\n{status}\n\n{instructions}";
    }

    [McpServerTool, Description("Mevcut Firestore bağlantısının durumunu kontrol eder.")]
    public static string CheckFirestoreConnection()
    {
        if (_firestoreDb == null)
        {
            return "Firestore bağlantısı kurulmamış. Önce AuthenticateFirestore tool'unu kullanın.";
        }

        try
        {
            var collections = _firestoreDb.ListRootCollectionsAsync();
            var collectionList = string.Join(", ", collections.Select(c => c.Id));

            return $"Firestore bağlantısı aktif!\n" +
                   $"Proje ID: {_currentProjectId}\n" +
                   $"Mevcut Collection'lar: {collectionList}";
        }
        catch (Exception ex)
        {
            return $"Firestore bağlantı hatası: {ex.Message}";
        }
    }

    [McpServerTool, Description("Firestore veritabanından belirtilen collection'daki tüm kayıtları getirir.")]
    public static async Task<string> GetCollectionDocuments(
        [Description("Collection adı.")] string collectionName,
        [Description("Maksimum döndürülecek kayıt sayısı (varsayılan: 50).")] int? limit = 50)
    {
        if (_firestoreDb == null)
        {
            return "Firestore bağlantısı kurulmamış. Önce AuthenticateFirestore tool'unu kullanın.";
        }

        try
        {
            // Collection referansını al
            var collection = _firestoreDb.Collection(collectionName);
            
            // Kayıtları getir
            var snapshot = await collection.Limit(limit ?? 50).GetSnapshotAsync();
            
            if (!snapshot.Any())
            {
                return $"'{collectionName}' collection'ında hiç kayıt bulunamadı.";
            }
            
            var results = new List<string>();
            int documentCount = 0;
            
            foreach (var document in snapshot)
            {
                documentCount++;
                var data = document.ToDictionary();
                
                var documentInfo = $"Döküman {documentCount}:\n" +
                                 $"ID: {document.Id}\n" +
                                 $"Oluşturulma Zamanı: {document.CreateTime}\n" +
                                 $"Güncellenme Zamanı: {document.UpdateTime}\n" +
                                 "Veriler:\n";
                
                foreach (var field in data)
                {
                    documentInfo += $"  {field.Key}: {field.Value}\n";
                }
                
                results.Add(documentInfo);
            }
            
            return $"'{collectionName}' collection'ından {documentCount} kayıt bulundu:\n\n" + 
                   string.Join("\n---\n", results);
        }
        catch (Exception ex)
        {
            return $"Firestore bağlantısında hata oluştu: {ex.Message}";
        }
    }

    [McpServerTool, Description("Firestore veritabanından belirtilen collection'da belirli bir alana göre filtreleme yapar.")]
    public static async Task<string> QueryCollectionDocuments(
        [Description("Collection adı.")] string collectionName,
        [Description("Filtrelenecek alan adı.")] string fieldName,
        [Description("Filtre değeri.")] string fieldValue,
        [Description("Maksimum döndürülecek kayıt sayısı (varsayılan: 50).")] int? limit = 50)
    {
        if (_firestoreDb == null)
        {
            return "Firestore bağlantısı kurulmamış. Önce AuthenticateFirestore tool'unu kullanın.";
        }

        try
        {
            // Collection referansını al
            var collection = _firestoreDb.Collection(collectionName);
            
            // Query oluştur
            var query = collection.WhereEqualTo(fieldName, fieldValue).Limit(limit ?? 50);
            var snapshot = await query.GetSnapshotAsync();
            
            if (!snapshot.Any())
            {
                return $"'{collectionName}' collection'ında '{fieldName}' = '{fieldValue}' koşuluna uygun kayıt bulunamadı.";
            }
            
            var results = new List<string>();
            int documentCount = 0;
            
            foreach (var document in snapshot)
            {
                documentCount++;
                var data = document.ToDictionary();
                
                var documentInfo = $"Döküman {documentCount}:\n" +
                                 $"ID: {document.Id}\n" +
                                 $"Oluşturulma Zamanı: {document.CreateTime}\n" +
                                 $"Güncellenme Zamanı: {document.UpdateTime}\n" +
                                 "Veriler:\n";
                
                foreach (var field in data)
                {
                    documentInfo += $"  {field.Key}: {field.Value}\n";
                }
                
                results.Add(documentInfo);
            }
            
            return $"'{collectionName}' collection'ında '{fieldName}' = '{fieldValue}' koşuluna uygun {documentCount} kayıt bulundu:\n\n" + 
                   string.Join("\n---\n", results);
        }
        catch (Exception ex)
        {
            return $"Firestore sorgusunda hata oluştu: {ex.Message}";
        }
    }

    [McpServerTool, Description("Firestore veritabanından belirtilen collection'ın istatistiklerini getirir.")]
    public static async Task<string> GetCollectionStats(
        [Description("Collection adı.")] string collectionName)
    {
        if (_firestoreDb == null)
        {
            return "Firestore bağlantısı kurulmamış. Önce AuthenticateFirestore tool'unu kullanın.";
        }

        try
        {
            // Collection referansını al
            var collection = _firestoreDb.Collection(collectionName);
            
            // Tüm kayıtları getir (sadece sayım için)
            var snapshot = await collection.GetSnapshotAsync();
            
            var documentCount = snapshot.Count;
            
            if (documentCount == 0)
            {
                return $"'{collectionName}' collection'ı boş.";
            }
            
            // İlk birkaç dökümanın alanlarını analiz et
            var fieldNames = new HashSet<string>();
            var sampleDocuments = snapshot.Take(10).ToList();
            
            foreach (var document in sampleDocuments)
            {
                var data = document.ToDictionary();
                foreach (var field in data.Keys)
                {
                    fieldNames.Add(field);
                }
            }
            
            return $"'{collectionName}' Collection İstatistikleri:\n" +
                   $"Toplam Döküman Sayısı: {documentCount}\n" +
                   $"Örnek Dökümanlarda Bulunan Alanlar: {string.Join(", ", fieldNames.OrderBy(f => f))}\n" +
                   $"Analiz Edilen Örnek Döküman Sayısı: {sampleDocuments.Count}";
        }
        catch (Exception ex)
        {
            return $"Firestore istatistiklerinde hata oluştu: {ex.Message}";
        }
    }

    [McpServerTool, Description("Firestore veritabanına yeni bir döküman ekler.")]
    public static async Task<string> AddDocument(
        [Description("Collection adı.")] string collectionName,
        [Description("Döküman verilerini JSON formatında girin.")] string jsonData)
    {
        if (_firestoreDb == null)
        {
            return "Firestore bağlantısı kurulmamış. Önce AuthenticateFirestore tool'unu kullanın.";
        }

        try
        {
            // JSON verisini parse et
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonData);
            if (data == null)
            {
                return "Geçersiz JSON formatı.";
            }

            // Collection referansını al
            var collection = _firestoreDb.Collection(collectionName);
            
            // Dökümanı ekle
            var documentReference = await collection.AddAsync(data);
            
            return $"Döküman başarıyla eklendi!\n" +
                   $"Collection: {collectionName}\n" +
                   $"Döküman ID: {documentReference.Id}\n" +
                   $"Eklenen Veriler: {jsonData}";
        }
        catch (Exception ex)
        {
            return $"Döküman ekleme hatası: {ex.Message}";
        }
    }
} 