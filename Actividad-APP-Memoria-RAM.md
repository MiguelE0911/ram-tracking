

Memoria RAM APP

# Laboratorio: desarrollo de una app para el diagnóstico de memoria RAM

**Asignatura:** Organización y Arquitectura de Computadoras  
**Modalidad:** Individual  
**Plataforma:** Windows 10 u 11  
**Duración sugerida:** Tres sesiones de dos horas  
**Nombre de la aplicación:** Ram Tracking

## Descripción de la actividad

Los estudiantes desarrollarán una aplicación con interfaz gráfica que permita conocer la capacidad de memoria RAM de una computadora, observar su utilización y generar un reporte de resultados.

##  Infografía: Crea una app de diagnóstico de memoria RAM (Consulta, Monitorea, Diagnostica

La aplicación se organizará en los tres componentes presentados en la imagen: **consulta, monitorea y diagnostica**. El estudiante deberá investigar cómo obtener la información del sistema operativo, programar sus funciones y comprobar que los datos mostrados sean coherentes.

**Consultar el consumo de memoria no permite demostrar que la RAM esté libre de fallos físicos.** Por esta razón, la aplicación deberá distinguir entre el monitoreo del uso y una prueba específica de memoria.

## Objetivo general

Desarrollar una aplicación de escritorio que consulte información de la memoria RAM, monitoree su utilización y facilite la documentación de un diagnóstico.

## Objetivos específicos

- Obtener información de la memoria instalada y disponible.

- Diseñar una interfaz gráfica clara y funcional.

- Representar el uso de RAM mediante indicadores y gráficas.

- Identificar los módulos de memoria cuando el sistema permita consultarlos.

- Orientar al usuario para ejecutar una prueba de memoria.

- Exportar un reporte con los resultados obtenidos.

## Situación que deben resolver

El laboratorio de informática necesita una aplicación sencilla que permita revisar el estado de utilización de la memoria de sus computadoras.

El usuario deberá identificar cuánta memoria tiene el equipo, cuánto está utilizando, cuánta permanece disponible y si conviene realizar una prueba adicional. La aplicación deberá presentar esta información de forma comprensible y permitir guardar un reporte.

## Funciones obligatorias de la aplicación

| **Componente**  | **Funciones requeridas**                                                                                                             |
|-----------------|--------------------------------------------------------------------------------------------------------------------------------------|
| **Consulta**    | Mostrar memoria física instalada, memoria utilizable por Windows, memoria en uso, memoria disponible y porcentaje de utilización.    |
| **Módulos**     | Consultar cantidad de módulos, capacidad por módulo, fabricante, tipo y velocidad configurada, cuando estos datos estén disponibles. |
| **Monitorea**   | Actualizar los indicadores y presentar una gráfica del porcentaje de utilización a lo largo del tiempo.                              |
| **Diagnostica** | Explicar la diferencia entre consumo elevado y fallos físicos; ofrecer instrucciones para ejecutar una prueba de memoria.            |
| **Reporte**     | Guardar un archivo con la identificación del equipo, fecha, hora, mediciones y observaciones.                                        |

Cuando un dato no pueda obtenerse, la aplicación mostrará **«No disponible»**. No deberá inventar valores ni sustituir información desconocida por cero. **Los Componentes presentados no quiere decir que no pueda incluir otros adicionales.**

## Desarrollo del laboratorio

### Diseño de la interfaz

La ventana principal deberá incluir:

- Nombre de la aplicación.

- Logo de la Aplicación

- Identificación del equipo.

- Tarjetas de capacidad, uso y memoria disponible.

- Barra de utilización con su porcentaje.

- Gráfica de las mediciones.

- Tabla de módulos detectados.

- Área de mensajes y recomendaciones.

- Botones para iniciar o detener el monitoreo, generar el reporte y consultar el diagnóstico.

Se puede tomar como referencia la distribución visual de la imagen. La prioridad será que la información resulte legible y fácil de interpretar.

### Programación de la consulta

Los estudiantes deberán programar las funciones que obtienen los datos de memoria.

La aplicación tendrá que:

1.  Consultar la información del equipo.

2.  Convertir los valores a unidades comprensibles.

3.  Mostrar las unidades junto a cada resultado.

4.  Controlar errores y consultas sin resultados.

5.  Mantener la interfaz operativa cuando un dato no esté disponible.

Para evitar ambigüedades, deberán indicar si utilizan **GB o GiB** y aplicar el mismo criterio en toda la aplicación.

En memoria DDR, la velocidad efectiva deberá expresarse en **MT/s** cuando corresponda; no se deberá presentar automáticamente ese valor como frecuencia en MHz.

### Monitoreo en tiempo real

La aplicación deberá actualizar las mediciones **cada dos segundos** y conservar al menos las últimas **60 muestras** para construir una gráfica.

Se utilizará el siguiente semáforo como criterio de la práctica:

| **Utilización**                | **Color** | **Mensaje**                                             |
|--------------------------------|-----------|---------------------------------------------------------|
| Menor de 70 %                  | Verde     | Utilización dentro del rango definido para la práctica. |
| Desde 70 % hasta menos de 85 % | Amarillo  | Utilización elevada; revisar las aplicaciones abiertas. |
| Desde 85 %                     | Rojo      | Utilización muy elevada; revisar la carga del equipo.   |

Estos rangos son orientativos y **no representan un diagnóstico de fallos físicos**.

El monitoreo no deberá bloquear la ventana. El usuario tendrá que poder moverla, detener las mediciones y generar el reporte mientras la aplicación permanece abierta.

### Apartado de diagnóstico

La aplicación mostrará esta aclaración:

El monitoreo informa sobre la utilización de la memoria. Para investigar posibles errores físicos se necesita una prueba específica de RAM.

El apartado deberá explicar cómo acceder al **Diagnóstico de memoria de Windows** y advertir que la prueba requiere reiniciar el equipo.

**No se programará un reinicio automático.** El usuario guardará su trabajo y decidirá cuándo ejecutar la prueba.

Después de realizarla, el estudiante deberá documentar el resultado. Si la aplicación no puede recuperarlo automáticamente, permitirá incorporarlo como una observación manual, claramente identificada.

### Generación del reporte

La aplicación deberá exportar un reporte en TXT o CSV que contenga:

| **Campo**             | **Contenido**                                 |
|-----------------------|-----------------------------------------------|
| Equipo                | Nombre de la computadora                      |
| Fecha y hora          | Momento de generación                         |
| Memoria instalada     | Capacidad física detectada                    |
| Memoria utilizable    | Capacidad reconocida para uso del sistema     |
| Memoria en uso        | Valor de la medición                          |
| Memoria disponible    | Valor de la medición                          |
| Utilización           | Porcentaje                                    |
| Módulos               | Información disponible por módulo             |
| Resumen del monitoreo | Mínimo, máximo y promedio de las muestras     |
| Prueba de memoria     | Realizada, no realizada o resultado pendiente |
| Observaciones         | Interpretación del estudiante                 |

El reporte no deberá declarar **«RAM sin fallos»** basándose únicamente en el porcentaje de utilización.

## Entregables

1.  Código fuente completo, desarrollado por los estudiantes.

2.  Aplicación ejecutable o instrucciones claras para ejecutarla.

3.  Captura de la ventana principal.

4.  Captura comparativa con el Administrador de tareas.

5.  Evidencia de la gráfica durante el monitoreo.

6.  Reporte generado por la aplicación.

7.  Evidencia del resultado de la prueba de memoria, o justificación si quedó pendiente.

8.  Conclusión sobre el funcionamiento de la app y las limitaciones de sus resultados.

**Resultado esperado:** una aplicación funcional que permita **consultar, monitorear y documentar** la memoria RAM, con resultados que el estudiante pueda explicar y defender durante la sustentación.
