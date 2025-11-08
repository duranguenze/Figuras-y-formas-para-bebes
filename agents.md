# Estado de la IA (agents.md)

Este archivo sirve para documentar y persistir el estado operativo de la(s) IA/Agentes del proyecto.

- Ultima_actualizacion:2025-11-0800:00
- Version_plantilla:1.0

##1. Resumen
- Descripcion: Aplicación tipo kiosco para bebés: pantalla completa, bloquea teclado excepto combinaciones permitidas; cada tecla dispara eventos visuales/sonoros configurables por JSON.
- Modo_actual (p.ej. planificacion/ejecucion/espera): planificacion
- Entorno (dev/test/prod): dev

##2. Agentes activos
- Agente:
 - Nombre: keyer-orchestrator
 - Rol/Responsabilidad: Coordinar captura de teclado, render de overlay y ejecución de acciones por tecla
 - Version:0.1.0
 - Owner: dev
 - Estado (idle/running/blocked): running

##3. Objetivos
- Largo_plazo: Estabilidad, seguridad de kiosk, personalización de acciones por tecla
- Corto_plazo: Implementar modo fullscreen, captura global de teclado, lectura de `keyer.config.json`, overlay visual y beep
- Hitos: Cargar config JSON, interceptar teclas, bloquear Alt+F4/Win, permitir salida por Ctrl+Alt+Supr y combo configurable

##4. Contexto y supuestos
- Requisitos/Restricciones: Windows, .NET9, WinForms, ejecución exclusiva a pantalla completa
- Dependencias externas: Ninguna por ahora
- Supuestos: Ejecución con permisos de usuario; bloqueo de WinKey/Alt+Tab tiene límites de sistema

##5. Memoria
- Corto_plazo: Definir hooks de teclado bajo nivel y overlay
- Largo_plazo: Persistir métricas de uso
- Conocimiento compartido (blackboard): `keyer.config.json`

##6. Variables de estado (clave-valor)
- planning.enabled: true
- retries.left:3
- rate_limit.window: "1m"

##7. Tareas
- En_curso: Diseñar arquitectura de hook de teclado y manejadores
- En_cola: Implementar lectura de config, overlay, beep, bloqueo de ventanas
- Completadas_reciente: Crear `keyer.config.json`

##8. Decisiones recientes
- Timestamp:2025-11-08T00:00:00Z
- Decision: Usar LowLevelKeyboardProc con SetWindowsHookEx para captura global
- Razonamiento_resumido: Necesario para bloquear teclas especiales y generar eventos sin foco
- Alternativas_consideradas: PreviewKeyDown solo con foco (insuficiente)

##9. Mensajeria
- Inbox (entradas relevantes):
 - "Quiero generar una aplicacion que se abra a pantalla completa, bloquee el teclado con excepcion de una combinacion de teclas, el objetivo es que un bebe le puedda picar a cada tecla y se lancen eventos que sean visuales y de sonidos, cada tecla o grupo de teclas despues las configurare para que hagan una animacion o muestren una imagen, o sonido, quiero agregar un archivo de configuracion en json, la forma de salir sera configurada en ese mismo archivo, debe bloquear todo, inclusive la tecla windows, la forma de salir seria con ctrl alt supr, y tambien con una combinacion pre configurada, no debe permitir salir con alt f4 ni similares, apunta este prompt en agents"
- Outbox (acciones/solicitudes emitidas): Crear archivo de configuración y plan de implementación

##10. Recursos y artefactos
- Datos/Archivos clave: `keyer.config.json`
- Conexiones/Endpoints: N/A
- Credenciales (no guardar secretos; referenciar almacen seguro): N/A

##11. Riesgos y bloqueos
- Riesgos: Bloquear completamente WinKey/Alt+Tab/Task Switcher no es100% soportado por apps normales; requiere modo quiosco/DirectInput/Accesos restringidos o políticas del SO
- Mitigaciones: Ejecutar como quiosco asignado, usar SetWindowsHookEx global, ocultar cursor, ventana top-most sin bordes
- Bloqueos_actuales: Validar permisos y comportamiento en distintas versiones de Windows

##12. Metricas/KPIs
- Exito_tarea (%):0
- Latencia_media: N/A
- Uso_memoria/CPU: N/A
- Errores_recientes: N/A

##13. Checkpoints y snapshots
- Ultimo_checkpoint_id: cp-2025-11-08-001
- Snapshots:

Ejemplo de snapshot (JSON):
```json
{
 "id": "cp-2025-11-08-001",
 "timestamp": "2025-11-08T00:00:00Z",
 "agentes": [
 { "nombre": "keyer-orchestrator", "estado": "running", "version": "0.1.0" }
 ],
 "objetivos": { "corto_plazo": ["modo fullscreen", "hook teclado", "overlay"], "largo_plazo": ["kiosk estable"] },
 "variables": { "retries.left":3, "planning.enabled": true },
 "tareas": { "en_curso": ["diseño hook"], "en_cola": ["lectura config"], "completadas": ["crear config json"] }
}
```

##14. Politica de persistencia
- Ubicacion_archivo: `agents.md` (raiz del repo)
- Rotacion: mantener N snapshots recientes
- Formato: Markdown con bloques JSON/YAML para estructuras
- Seguridad: no incluir PII ni secretos; usar referencias a almacenamiento seguro

##15. Convenciones de edicion
- Agregar entradas al final de cada seccion manteniendo orden cronologico
- Usar timestamps en UTC ISO-8601
- Revisar y actualizar `Ultima_actualizacion` en cada cambio
