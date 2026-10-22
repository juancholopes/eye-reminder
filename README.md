# Eye Reminder

App de escritorio en .NET 10 + WinUI 3 (Windows App SDK 2.5) que aplica la regla **20-20-20**:
cada 20 minutos aparece un notch oscuro en la parte superior de la pantalla con una cuenta de
20 segundos para mirar algo a 20 pies (~6 m). Si mueves el mouse, la cuenta vuelve a 20; al
llegar a cero el notch desaparece y empieza otro ciclo.

## Instalar

Instala para el usuario actual en `%LOCALAPPDATA%\Programs\EyeReminder`, crea el acceso directo
en el menú Inicio y abre la app (agrega `-Startup` para que arranque al iniciar sesión):

```bash
pwsh -File scripts/install.ps1
```

## Ejecutar desde el código

```bash
dotnet run --project EyeReminder
```

Modo demo (primer recordatorio a los 15 s en lugar de 20 min):

```bash
dotnet run --project EyeReminder -- --demo
```

## Arquitectura (MVVM)

| Capa | Archivos | Responsabilidad |
|---|---|---|
| Model | `Models/ReminderSettings.cs` | Parámetros de la regla (intervalo, duración, distancia). |
| Services | `Services/ReminderScheduler.cs`, `Services/MouseActivityService.cs` | Temporizador de 20 min y detección global de movimiento del mouse. |
| ViewModels | `ViewModels/MainViewModel.cs`, `ViewModels/IslandViewModel.cs` | Ciclo trabajo → descanso y cuenta de 20 s que se reinicia con el mouse. |
| Views | `Views/MainWindow.xaml`, `Views/IslandWindow.xaml` | Ventana principal y notch siempre visible (sin foco, fuera de Alt+Tab). |
| Interop | `Interop/NativeMethods.cs` | Win32: posición del cursor, forma del notch, ventana *topmost*. |

`App.xaml.cs` es la raíz de composición: crea los servicios, los ViewModels y las vistas.

## Notas

- La app no está empaquetada (no requiere MSIX) e incluye el runtime de Windows App SDK.
- El notch se queda por encima de todas las ventanas normales y de otras ventanas "siempre visible",
  pero no puede dibujarse sobre juegos en pantalla completa exclusiva.
- Cerrar la ventana principal solo la oculta: la app sigue en la bandeja del sistema. Clic en el
  icono para abrirla; clic derecho para Pausar/Reanudar, Descansar ahora o Salir.
- El notch usa los colores de WinUI 3 y cambia con el modo claro u oscuro de Windows.
