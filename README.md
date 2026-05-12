# 📌 LiveTalk – Real-Time Chat & Calling Platform

LiveTalk is a **real-time communication platform** that delivers **one-to-one chat, group chat, and voice/video calling**, built using **ASP.NET Core Web API**, **SignalR**, and **SQL Server**, with a lightweight frontend powered by **HTML, CSS, and JavaScript**.

The project is designed with **scalability, performance, security, and clean architecture** in mind, making it similar in concept to modern messaging platforms like **WhatsApp** or **Messenger**.

---
## 🚀 Live Demo

🔗 **Live Demo:**  
👉 https://live-talk-demo.vercel.app

🎬 [Watch / Download Project Demo Video](https://raw.githubusercontent.com/Ayamohamed2/LiveTalk-Website/master/LiveTalk-Video.mp4)

---

## 🛠 Tech Stack

### 🔹 Backend
- ASP.NET Core Web API  
- SignalR (Real-time communication)  
- Entity Framework Core  
- SQL Server  
- ASP.NET Identity  
- JWT Authentication  
- API Versioning
- Serilog Logging
- xUnit Testing
- Integration Testing
- Rate Limiting  
- Background Services
- Health Checks  
- Custom Middlewares 
 

---

## 🧩 Project Architecture

- Clean Architecture principles  
- Repository Pattern  
- Unit of Work Pattern  
- DTOs for API communication  
- Clear separation of concerns  
- Scalable real-time hubs  

---

## 🔐 Security Features

- JWT Authentication & Authorization  
- Token Blacklisting (Logout / Revoked Tokens)  
- Rate Limiting:
  - Authentication
  - Messaging
  - API requests
  - Group operations
- Block / Unblock users  
- Secure file upload handling  
- Authorization at Controller & Hub levels  

---
## 🔑 Authentication & Account Management

LiveTalk implements a **secure and production-ready authentication system** using **ASP.NET Identity** and **JWT**, with full account lifecycle management.

### Authentication Features
- User registration & login  
- JWT Access Token authentication  
- Refresh Token mechanism for session renewal  
- Secure token storage & validation  
- Token expiration handling  
- Logout with token revocation (blacklisting)  

### Account Security & Recovery
- Email confirmation after registration  
- Forgot password flow  
- Reset password using secure tokens  
- Password validation & hashing  
- Protection against brute-force attacks  

### Account Protection
- Rate limiting for authentication endpoints  
- Revoked token validation middleware  
- Authorization policies at API & SignalR level  

## 💬 One-to-One Chat Features

- Send text messages  
- Send media messages:
  - Images
  - Audio
  - Video
- Message replies  
- Typing indicator  
- Online / offline user status  
- Message delivery states:
  - Sent
  - Delivered
  - Read
- Unread message count  
- Message deletion:
  - Delete for me
  - Delete for everyone (time-limited)
- Clear chat  
- Block & unblock users  
- Chat list includes:
  - Last message
  - Unread count
  - Last seen
  - Block status  

---

## 👥 Group Chat Features

- Create and manage groups  
- Join / leave groups  
- Group membership validation  
- Group typing indicators  
- Group message deletion  
- Group message read tracking  
- Automatic group rejoin on reconnect  
- Real-time group events using SignalR  

---

## 📞 Voice & Video Calling

- One-to-one voice calls  
- One-to-one video calls  
- Call states:
  - Ringing
  - Active
  - Rejected
  - Missed
  - Busy
  - Ended
- Call duration tracking  
- Busy detection  
- Mute / unmute support  
- WebRTC signaling:
  - Offer
  - Answer
  - ICE candidates
- Automatic cleanup on disconnect  

---

## 👤 User Profile Management

- View user profile  
- Update profile information  
- Upload / update profile image  
- Default image handling  
- Last seen tracking  

---

## ⚡ Real-Time Communication (SignalR)

### 🔹 ChatHub
- Online / offline presence tracking  
- Multiple connections per user  
- Typing indicators (one-to-one & groups)  
- Group join / leave handling  
- User status broadcasting  
- Real-time message events  

### 🔹 CallHub
- Real-time call signaling  
- Active call tracking  
- Call lifecycle management  
- Automatic call cleanup on disconnect   

---

## 🧪 Testing

LiveTalk includes comprehensive testing to ensure API reliability and maintainability.

### Testing Types
- Unit Testing using xUnit & Moq
- Integration Testing using WebApplicationFactory
- Controller endpoint testing
- Service layer testing
- HTTP status code verification

### Testing Tools
- xUnit
- Moq
- FluentAssertions
- ASP.NET Core Integration Testing
  
## 🧯 Custom Middlewares

- Global Exception Handling Middleware  
- Blacklisted Token Middleware  
- Centralized error responses  
- Secure token validation
  
---

## 📊 Logging & Monitoring

- Structured logging using Serilog
- Request / response logging
- Centralized exception logging
- Health Check endpoints for monitoring system health
- Performance-friendly logging configuration

---

## 🔀 API Versioning

- Versioned REST APIs
- Supports scalable API evolution
- Route-based API versioning
- Example:
  - `/api/v1/...`
  - `/api/v2/...`
    
---

## 🗄 Database

- SQL Server  
- Optimized relational schema  
- Indexed queries for high chat performance  
- Soft delete strategy for messages  
- Efficient unread message count queries
  
---

## ❤️ Health Checks

- Application health monitoring
- Database connectivity checks
- Ready for production deployment monitoring
---

## ⭐ Project Highlights

- WhatsApp-like real-time experience  
- Scalable SignalR architecture  
- Secure authentication & authorization  
- Clean and maintainable backend design  
- Production-ready API features  

---

## 📬 Contact

If you’d like to discuss this project or collaborate, feel free to reach out 🚀
