# Despliegue en Windows/IIS

## Publicación de la API .NET
1. Instalar .NET 8/9 SDK en el servidor.
2. Ejecutar `dotnet publish src/Ocr.Api/Ocr.Api.csproj -c Release -o publish` en la raíz de `ocr-system`.
3. Configurar un sitio en IIS apuntando a la carpeta `publish`. Habilitar modo **in-process** o **out-of-process** según política.
4. Establecer el pool de aplicaciones con arquitectura x64 y límites de request (por ejemplo `maxAllowedContentLength` acorde a uploads de 100 MB).
5. Copiar `appsettings.Production.json` con las secciones `Workers`, `TempStorage`, `Azure`, `Db`, `RateLimit`.
6. Habilitar logging con rollo diario y verificación de encabezado `X-Correlation-Id` en registros.

## Configuración de workers PaddleOCR
1. Instalar Python 3.10+ y dependencias con `pip install -r requirements.txt` en `src/Ocr.Worker.Paddle` (o ejecutar `scripts/setup_python_worker.ps1`).
2. Generar stubs gRPC (si no existen) ejecutando `..\scripts\generate_protos.ps1`. En Windows puede usarse un entorno separado con `requirements.protos.txt` pasando `-InstallProtoTools` al script de instalación.
3. Instalar NSSM o usar `sc.exe` para crear el servicio de Windows.
4. Ejecutar `run.ps1 -ServiceName OcrWorker1 -Port 50051 -WorkingDirectory C:\ocr-system\src\Ocr.Worker.Paddle`.
5. Repetir para cada worker cambiando `-Port` y actualizando `Workers:Endpoints` en `appsettings.Production.json`.

### Probar el worker en consola
1. Crear/actualizar el entorno con `.\scripts\setup_python_worker.ps1 -WorkerPath C:\ocr-system\src\Ocr.Worker.Paddle` (deja el intérprete en `.\.venv\Scripts\python.exe`).
2. Desde `C:\ocr-system\src\Ocr.Worker.Paddle`, definir variables para la sesión de depuración:
   ```powershell
   $env:WORKER_PORT = 50051
   $env:WORKER_LANG = "es"      # o "en", etc.
   $env:WORKER_CONCURRENCY = 1  # opcional
   $env:PADDLE_USE_ANGLE_CLS = "true"
   ```
3. Arrancar el worker directamente: `.\.venv\Scripts\python.exe server.py`. Si se necesitan logs más verbosos, ejecutar con `set PYTHONVERBOSE=1` antes del comando.

### Crear/actualizar el servicio de Windows
`src\Ocr.Worker.Paddle\scripts\run.ps1` usa NSSM para apuntar explícitamente al Python del virtualenv y establece las variables de entorno del worker. Ejemplos:

- Instalar con el virtualenv por defecto en la carpeta del worker:
  ```powershell
  .\scripts\run.ps1 -ServiceName OcrWorker1 -Port 50051 `
    -WorkingDirectory C:\ocr-system\src\Ocr.Worker.Paddle
  ```

- Instalar especificando otra ruta de venv y configuración:
  ```powershell
  .\scripts\run.ps1 -ServiceName OcrWorker1 -Port 50051 `
    -WorkingDirectory C:\ocr-system\src\Ocr.Worker.Paddle `
    -VenvPath C:\venvs\ocrworker `
    -Lang es -Concurrency 1 -DisableAngleCls:$false
  ```

Si el servicio muestra el error genérico "verifique si la aplicación es un servicio", validar:
- Que `server.py` exista en el `WorkingDirectory`.
- Que el Python configurado (`-PythonPath`, `-VenvPath` o `.venv`) tenga instaladas las dependencias.
- Que NSSM esté disponible en PATH o junto al script.

## Notas operativas
- Verificar conectividad saliente hacia Azure Cognitive Services antes de habilitar el fallback.
- La tabla `AzureUsage` controla el límite mensual; programar respaldos y supervisión.
- El worker es de concurrencia 1 por diseño para minimizar consumo de VRAM; escalar agregando instancias adicionales.
- Actualizar firewall para exponer únicamente los puertos gRPC internos y HTTPS público de la API.
