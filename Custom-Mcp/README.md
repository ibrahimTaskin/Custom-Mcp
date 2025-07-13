# MCP Custom Tool Ekleme ve Yayına Alma Adımları

Aşağıdaki adımlar, projeye yeni bir tool (ör. FirestoreTools gibi) eklediğinizde, bu tool'un MCP server ve docker ortamında görünmesi için yapılması gereken işlemleri özetler.

---

## 1. Tool Dosyasını Ekleyin
- Yeni tool'unuzu `Custom-Mcp/Tools/` klasörüne ekleyin.
- Sınıfınızın başına `[McpServerToolType]` ve her fonksiyonun başına `[McpServerTool]` attribute'larını ekleyin.

## 2. Projeyi Build Edin
```sh
dotnet build Custom-Mcp/Custom-Mcp.csproj
```
- Build işlemi başarılı olmalı, hata almamalısınız.

## 3. Docker Image'ını Yeniden Oluşturun
- MCP sunucusu docker ile çalışıyorsa, yeni eklenen tool'ların docker image'ına dahil olması için image'ı tekrar build edin:

```sh
cd Custom-Mcp
# Gerekirse --no-cache ile
# docker build --no-cache -t mcp-weather-server .
docker build -t mcp-weather-server .
```

## 4. Docker Container'ı Yeniden Başlatın
- Yeni image ile container'ı başlatın veya cursor/mcp arayüzünden tool'ları tekrar listeleyin.

## 5. Tool'ları Kontrol Edin
- MCP arayüzünde veya API üzerinden yeni tool'un görünüp görünmediğini kontrol edin.

---

### Notlar
- `.env` ve `credentials` gibi dosyaların eksiksiz olduğundan emin olun.
- Tool'larınızın doğru attribute'larla işaretlendiğinden emin olun.
- Her değişiklik sonrası docker image'ını yeniden build etmeden yeni tool'lar docker ortamında görünmez! 