# Estado de la IA (agents.md)

Este archivo sirve para documentar y persistir el estado operativo de la(s) IA/Agentes del proyecto.

- Ultima_actualizacion:2025-11-0801:30Z
- Version_plantilla:1.0

##1. Resumen
- Descripcion: Aplicación kiosco para bebés en WinForms (.NET9) que captura teclado global, bloquea combinaciones no permitidas y muestra imágenes o figuras geométricas animadas simples; configurable vía JSON.
- Modo_actual: construccion
- Entorno: dev

##2. Agentes activos
- Agente:
 - Nombre: keyer-orchestrator
 - Rol: Captura de teclado, render de overlay, selección de acción (imagen/figura/sonido)
 - Version:0.3.0
 - Owner: dev
 - Estado: running

##3. Objetivos
- Largo_plazo: Robustez, modo kiosco endurecido, personalización avanzada (animaciones, sonidos por grupo)
- Corto_plazo: Límite de figuras acumuladas, limpieza manual, soporte color configurable para bordes/figuras
- Hitos alcanzados: Fullscreen, hook global, bloqueo Win/Alt+F4/Alt+Tab (best-effort), salida Escape, carga remota de imágenes, figuras avanzadas (Heart, Star, Polygon), acumulación opcional de figuras, fondo configurable.

##4. Contexto y supuestos
- Restricciones: No bloqueo total gestos touchpad sin políticas del SO.
- Dependencias: user32.dll hook, HttpClient para descarga de imágenes.
- Supuestos: Usuario tiene permisos para imágenes usadas.

##5. Memoria
- Corto_plazo: Añadir límite y limpieza de figuras acumuladas.
- Largo_plazo: Persistir estadísticas de teclas pulsadas.
- Blackboard: `keyer.config.json`

##6. Variables de estado
- visual.accumulateShapes: true
- overlay.exitCombo: Escape
- shapes.supported: [Rectangle,Ellipse,Triangle,Diamond,Heart,Star,Polygon]

##7. Tareas
- En_curso: Diseño límite/gestión de lista de figuras acumuladas
- En_cola: Color configurable de figura/borde; botón invisible de limpieza; métricas
- Completadas_reciente: Integrar imágenes remotas; figuras nuevas (Heart, Star, Polygon); acumulación; fondo configurable; estrella variable; polígono5-8 lados

##8. Decisiones recientes
-2025-11-08T01:15Z: Usar HttpClient y caché local para imágenes remotas -> Minimiza latencia y reuso.
-2025-11-08T01:20Z: Representar figuras mediante lista persistente cuando accumulateShapes=true -> permite efecto visual acumulativo.
-2025-11-08T01:25Z: Bordes transparentes (alpha0) para eliminar líneas intermedias en Bezier corazón -> mejora estética.

##9. Mensajeria
- Inbox: Solicitud de figuras adicionales (corazón, estrella, polígono), acumulación, cambio colores overlay y fondo.
- Outbox: Actualizaciones de config, implementación de nuevas figuras y propiedades.

##10. Recursos y artefactos
- keyer.config.json (añadidos: formBackColor, shapeBorderThickness, accumulateShapes)
- assets/cache (descarga remota temporal)

##11. Riesgos y bloqueos
- Riesgo: Hook podría ser interferido por antivirus/políticas.
- Riesgo: Descarga de imágenes externas sin verificación licencia.
- Mitigación: Uso de placeholders; advertencia de licencias.
- Bloqueo: Gestos del sistema (task view) no bloqueables desde app.

##12. Metricas/KPIs (pendiente de implementación)
- total_key_presses:0
- shapes_rendered:0
- images_shown:0

##13. Checkpoints y snapshots
- Ultimo_checkpoint_id: cp-2025-11-08-002
- Snapshots:
```json
{
 "id": "cp-2025-11-08-002",
 "timestamp": "2025-11-08T01:30:00Z",
 "version_app": "0.3.0",
 "config_flags": { "accumulateShapes": true, "exitCombo": "Escape" },
 "shapes": ["Rectangle","Ellipse","Triangle","Diamond","Heart","Star","Polygon"],
 "recent_decisions": ["remote_image_cache","accumulate_list","transparent_border"],
 "completed": ["imagenes_remotas","figuras_avanzadas","acumulacion","fondo_configurable","estrella_variable","poligono_5_8"],
 "pending": ["limite_figuras","color_figura_config","limpieza_manual","metricas"]
}
```

##14. Politica de persistencia
- Rotacion: mantener últimos5 snapshots.

##15. Convenciones de edicion
- Timestamps en UTC ISO-8601.
- Actualizar Ultima_actualizacion con cada modificación.
