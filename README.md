# 🏍️ Real-Time Courier Telemetry & Logistics System

> 🚀 A real-time logistics and courier tracking simulation built with **Clean Architecture**, designed for scalability, performance, and modern distributed systems.

---

## 📌 Overview

This project demonstrates a **real-time courier tracking system** integrated into a microservices-ready architecture.
It focuses on **event-driven communication**, **geospatial data processing**, and **high-performance telemetry streaming**.

---

## 🧱 Architecture

Built strictly following **Clean Architecture principles**:

```
Domain → Application → Infrastructure → Presentation
```

✔ Separation of concerns
✔ High testability
✔ Framework-independent core logic

---

## ✨ Core Features

### 📡 Real-Time Communication

* Powered by **SignalR (WebSockets)**
* Instant courier location updates
* No HTTP polling overhead

### 🗺️ Geospatial Telemetry Processing

* Redis Geo-Spatial indexing for fast location queries
* Efficient handling of high-frequency coordinate streams

### 🗄️ Dual Persistence Strategy

| Storage Type    | Technology | Purpose                             |
| --------------- | ---------- | ----------------------------------- |
| Relational DB   | PostgreSQL | Source of truth (couriers, history) |
| In-Memory Cache | Redis      | Real-time location tracking         |

### 🤖 Autonomous Simulation

* `.NET BackgroundService`
* Generates dynamic courier movement
* Simulates real-world delivery scenarios

### ⚡ Reactive Frontend

* Angular SPA (Standalone Components)
* RxJS for reactive state management
* Leaflet.js for live map rendering

---

## 🛠️ Tech Stack

### 🔧 Backend

* .NET 8 / ASP.NET Core Web API
* Entity Framework Core (Code-First)
* PostgreSQL
* Redis (StackExchange.Redis)
* SignalR

### 🎨 Frontend

* Angular
* Tailwind CSS
* Leaflet.js
* RxJS

### ⚙️ DevOps

* Docker & Docker Compose
* Redis Insight
* DBeaver

---

## 📂 Project Structure

```
📦 Logistics-Tracking-System
 ┣ 📂 Backend        # Clean Architecture (.NET)
 ┣ 📂 Frontend       # Angular SPA
 ┣ 📜 docker-compose.yml
 ┗ 📜 README.md
```

---

## 🚀 Getting Started

### ✅ Prerequisites

* Docker
* .NET 8 SDK
* Node.js & npm
* Angular CLI

---

## ⚙️ Installation & Run

### 1️⃣ Start Infrastructure

```bash
docker compose up -d
```

---

### 2️⃣ Run Backend

```bash
# Apply migrations
dotnet ef database update --project Repository --startup-project Presentation

# Start API + Worker
cd Presentation
dotnet run
```

---

### 3️⃣ Run Frontend

```bash
cd Frontend
npm install
ng serve -o
```

---

## 🧠 System Design Highlights

* 📍 Real-time geo-tracking with Redis
* 🔄 Event-driven architecture
* ⚡ High-performance data streaming
* 🧩 Microservices-ready design

---

## 📸 (Optional) Screenshots

> You can add UI screenshots here for better presentation.

```
![Dashboard](./docs/dashboard.png)
```

---

## 🤝 Contributing

Pull requests are welcome. For major changes, please open an issue first.

---

## 📄 License

This project is open-source and available under the **MIT License**.

---

## 💡 Final Notes

This project is ideal for demonstrating:

* Clean Architecture mastery
* Real-time system design
* Distributed data handling
* Modern full-stack engineering

---
