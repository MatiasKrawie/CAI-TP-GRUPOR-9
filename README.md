# 🛒 Sistema E-Commerce - Arquitectura de Microservicios

Este proyecto consiste en una solución integral de comercio electrónico diseñada bajo una arquitectura distribuida de microservicios, utilizando **.NET 10 Web API**, persistencia ligera optimizada en **SQLite**, y contenedores **Docker** para su orquestación y despliegue unificado.

---

## 🛠️ Tecnologías y Características Globales
* **.NET 10.0 (Web API)** con C# estructural y modular.
* **Identificadores Únicos Globales (GUID)** para garantizar la consistencia relacional e integridad transactorial entre servicios independientes.
* **Documentación Automatizada con Swagger**: Configuración completa mediante Comentarios XML (///) y segregación por tags de dominio de negocio.
* **Monitoreo de Infraestructura**: Implementación nativa de *Health Checks* con endpoints de diagnóstico rápido (/health, /health/live, /health/ready).
* **Trazabilidad de Peticiones Distribuidas**: Registro estructurado mediante **Serilog** con inyección de *Correlation ID* transversal a toda la malla de servicios.
* **Manejo Estandarizado de Errores**: Middlewares globales para respuestas homogéneas basadas en la especificación **RFC 7231**.

---

## 🏗️ Mapa de la Solución (5 Microservicios)

El ecosistema se compone de los siguientes componentes acoplados de forma laxa:

1. **Users.Api**: Registro, autenticación, control de perfiles y bloqueos preventivos por fraude (Tag: *Gestión de Usuarios*).
2. **Products.Api**: Catálogo general de productos, administración de precios, descripciones e inventario físico (Tag: *Catálogo de Productos*).
3. **Cart.Api**: Gestión temporal de carritos de compras vinculados unívocamente a los usuarios mediante GUID (Tag: *Gestión de Carritos de Compra*).
4. **Orders.Api**: Procesamiento de órdenes de compra, cálculo de totales, transacciones y cambio de estados del pedido (Tag: *Procesamiento de Órdenes*).
5. **Notifications.Api**: Despacho de alertas, confirmaciones de compra y comunicación asincrónica de eventos del sistema (Tag: *Servicio de Notificaciones*).

---

## 🚀 Requisitos Previos

Antes de ejecutar el proyecto, asegúrate de contar con:
* [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
* [Docker Desktop](https://www.docker.com/products/docker-desktop/)
* [Visual Studio 2022](https://visualstudio.microsoft.com/) (Última actualización con soporte .NET 10) o VS Code.
## 💻 Cómo Ejecutar el Proyecto

Puedes iniciar el entorno completo de dos maneras:

### Opción A: Desde Visual Studio (Entorno de Desarrollo)
1. Abrir la solución principal `ECommerce.sln`.
2. Hacer clic derecho sobre el nodo de la Solución (raíz) y seleccionar **Propiedades**.
3. En **Proyectos de inicio comunes**, activar la opción **Proyectos de inicio múltiples**.
4. Configurar la acción como **Iniciar** para los 5 proyectos core de la arquitectura: `Users.Api`, `Products.Api`, `Cart.Api`, `Orders.Api` y `Notifications.Api`.
5. Presionar **F5** (o el botón **Play**). Se levantarán simultáneamente las terminales de logs y las interfaces de Swagger en tu navegador.

### Opción B: Orquestación con Docker Compose (Despliegue Rápido)
Para compilar y levantar toda la arquitectura de infraestructura con un único comando, ejecutá en la terminal de la raíz:
> docker-compose up --build -d

Para dar de baja y limpiar los contenedores locales, ejecutá:
> docker-compose down

## 🔍 Puertos y URLs de Verificación

Cuando la solución se encuentre en ejecución, podrás auditar y probar cada componente de manera independiente en los siguientes endpoints locales:

### 📑 Documentación de APIs (Swagger UI)
* **Users.Api**: https://localhost:7001/swagger
* **Products.Api**: https://localhost:7002/swagger
* **Cart.Api**: https://localhost:7003/swagger
* **Orders.Api**: https://localhost:7004/swagger
* **Notifications.Api**: https://localhost:7005/swagger

### 🩺 Monitores de Salud (Health Checks)
Cada uno de los 5 microservicios cuenta con sus propios chequeos de vida expuestos en:
* **Estado General de Infraestructura**: GET /health
* **Verificación de Liveness (API Viva)**: GET /health/live
* **Verificación de Readiness (Dependencias/Bases de Datos OK)**: GET /health/ready

---

## 🧪 Flujo de Pruebas Integrales de Negocio

Para constatar que la interacción entre los 5 componentes es perfecta, seguí este flujo lógico de pruebas:

1. **Alta de Usuario**: Genera un usuario en `Users.Api` (`POST /api/users/register`). Copiá el `Id` (GUID) resultante.
2. **Consulta de Stock**: Obtené el GUID de un producto disponible desde `Products.Api` (`GET /api/products`).
3. **Carga del Carrito**: En `Cart.Api`, usá el GUID del usuario y del producto para añadir ítems mediante el `POST /api/cart/{userId}/items`.
4. **Cierre de Orden**: Dirígete a `Orders.Api` para consolidar la compra impactando el carrito del usuario. Esto disparará internamente la lógica de negocio.
5. **Auditoría de Notificaciones**: Verificá en la consola o logs de `Notifications.Api` cómo se procesa y registra la alerta de "Orden Creada Exitosamente" vinculada al ID del cliente.


## DIAGRAMA DE ARQUITECTURA
<img width="886" height="680" alt="image" src="https://github.com/user-attachments/assets/6d22780f-3f5d-4ab8-8f91-854647991112" />
