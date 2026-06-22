# Guía Detallada: SOLID aplicado al Order Service (Backend - C#)

Esta guía detalla los principios de diseño **SOLID** que hemos implementado en el microservicio de órdenes (`Bookstore.OrderService`), indicando los archivos modificados, dónde se aplican y la justificación técnica de cada decisión para que sirva como material educativo.

---

## 1. Single Responsibility Principle (SRP) - Principio de Responsabilidad Única

> **Definición:** Una clase debe tener una, y solo una, razón para cambiar.

### ¿Dónde se aplica?
*   **[Order.cs](file:///c:/Users/miche/Desktop/Bookstore/Bookstore.OrderService/src/OrderService/Data/Order.cs) (MODIFICADO):** La entidad de dominio ahora asume la única responsabilidad de salvaguardar su estado y calcular su precio total.
*   **[OrderRepository.cs](file:///c:/Users/miche/Desktop/Bookstore/Bookstore.OrderService/src/OrderService/Data/Repositories/OrderRepository.cs) (NUEVO):** Asume exclusivamente la responsabilidad de almacenar y recuperar las órdenes de la base de datos.
*   **[CreateOrder.cs](file:///c:/Users/miche/Desktop/Bookstore/Bookstore.OrderService/src/OrderService/Features/Orders/Commands/CreateOrder.cs) (MODIFICADO):** El Handler solo orquesta el flujo (inicializar orden, persistir mediante repositorio y disparar evento de integración a MassTransit).

### ¿Por qué?
Antes de la refactorización, el `CreateOrderCommandHandler` se encargaba de mapear inputs, instanciar los ítems uno a uno, calcular la sumatoria matemática del `TotalAmount`, guardar en la base de datos y publicar en la cola de mensajería. Esto violaba SRP. Ahora, el cálculo matemático se encapsula en el dominio (`Order`), el guardado físico en el repositorio (`OrderRepository`), y el handler solo coordina los pasos principales.

---

## 2. Open/Closed Principle (OCP) - Principio de Abierto/Cerrado

> **Definición:** Las entidades de software deben estar abiertas para su extensión, pero cerradas para su modificación.

### ¿Dónde se aplica?
*   **[Order.cs](file:///c:/Users/miche/Desktop/Bookstore/Bookstore.OrderService/src/OrderService/Data/Order.cs):** En el método de comportamiento **`AddItem`**.
*   **[OrderRepository.cs](file:///c:/Users/miche/Desktop/Bookstore/Bookstore.OrderService/src/OrderService/Data/Repositories/OrderRepository.cs):** La consulta de lectura incluye la colección de ítems y se aísla de los controladores.

### ¿Por qué?
Si en el futuro decidimos cambiar las reglas de adición de ítems a un pedido (por ejemplo, validar que no se agreguen cantidades negativas, aplicar una comisión por servicio, o verificar que el precio unitario coincida con el catálogo), realizamos la modificación **dentro del método `AddItem` de la clase `Order`**. 
El código de `CreateOrderCommandHandler` permanece **cerrado a la modificación**, ya que solo llama a `order.AddItem(...)` de forma transparente. Hemos extendido el comportamiento del dominio sin alterar la capa de aplicación.

---

## 3. Liskov Substitution Principle (LSP) - Principio de Sustitución de Liskov

> **Definición:** Las clases derivadas o implementaciones deben ser sustituibles por sus tipos base sin alterar el comportamiento correcto del programa.

### ¿Dónde se aplica?
En el uso de **[Mock de IOrderRepository](file:///c:/Users/miche/Desktop/Bookstore/Bookstore.OrderService/tests/OrderService.Tests/OrderHandlerTests.cs#L104)** en nuestras pruebas unitarias.

### ¿Por qué?
En la prueba unitaria `CreateOrder_Should_Call_Repository_Add_And_SaveChanges`, inyectamos un objeto mock simulado de `IOrderRepository` creado con Moq:
```csharp
var mockRepository = new Mock<IOrderRepository>();
var handler = new CreateOrderCommandHandler(mockRepository.Object, mockPublishEndpoint.Object);
```
El `CreateOrderCommandHandler` se ejecutó sin errores usando la interfaz simulada. Esto prueba que el sistema tolera perfectamente sustituir el repositorio de base de datos real por un sustituto virtual en memoria (LSP).

---

## 4. Interface Segregation Principle (ISP) - Principio de Segregación de Interfaces

> **Definición:** Los clientes no deben ser obligados a depender de interfaces que no utilizan.

### ¿Dónde se aplica?
*   **[IOrderRepository.cs](file:///c:/Users/miche/Desktop/Bookstore/Bookstore.OrderService/src/OrderService/Data/Repositories/IOrderRepository.cs):** Contrato limpio y exclusivo para la persistencia de pedidos.

### ¿Por qué?
Mantener un contrato pequeño e independiente para órdenes evita que mezclemos métodos ajenos (por ejemplo, consultas de inventario, stock, catálogo de libros o perfiles de clientes). La interfaz define solo lo que el contexto de pedidos necesita usar, previniendo que los componentes dependan de métodos superfluos.

---

## 5. Dependency Inversion Principle (DIP) - Principio de Inversión de Dependencias

> **Definición:** Los módulos de alto nivel no deben depender de módulos de bajo nivel. Ambos deben depender de abstracciones.

### ¿Dónde se aplica?
En el desacoplamiento de los Handlers de escritura y lectura:
*   [CreateOrderCommandHandler](file:///c:/Users/miche/Desktop/Bookstore/Bookstore.OrderService/src/OrderService/Features/Orders/Commands/CreateOrder.cs) y [GetOrdersQueryHandler](file:///c:/Users/miche/Desktop/Bookstore/Bookstore.OrderService/src/OrderService/Features/Orders/Queries/GetOrders.cs) ya no conocen `OrderDbContext`.
*   Dependen de `IOrderRepository`.
*   Registro en **[Program.cs](file:///c:/Users/miche/Desktop/Bookstore/Bookstore.OrderService/src/OrderService/Program.cs)**:
    ```csharp
    builder.Services.AddScoped<IOrderRepository, OrderRepository>();
    ```

### ¿Por qué?
Al invertir la dependencia, la capa de aplicación (los Handlers) dicta el contrato (`IOrderRepository`) y la capa de infraestructura (el acceso a datos con EF Core) se adapta para cumplirlo. Si la infraestructura de datos cambia en el futuro, no se ve afectada la lógica de negocio de creación de pedidos.
