# 🛒 BidCommerce

**BidCommerce** is an intelligent online auction and e-commerce platform where users can list, bid, and buy products in real-time. Packed with modern features like AI-assisted listings and live chat between users, BidCommerce makes online selling smarter and faster

---

## 🌟 Key Features

- 🔐 **Secure User Authentication**  
  Register, login, and manage profiles with ASP.NET Identity.

- 📦 **Product Listings with Image Uploads**  
  Create detailed listings with multiple product images.

- 📂 **Dynamic Category System**  
  Products are grouped into categories for intuitive browsing.

- 🕒 **Real-Time Auction Engine**  
  Bid live against other users, with countdown timers and bid history.

- ⚡ **Buy Now Option**  
  Instantly purchase without bidding.

- 🧠 **AI-Powered Product Descriptions** *(Beta)*  
  Just upload images — our AI suggests a full product details automatically.

- 💬 **Live Chat Between Users** *(In Progress)*  
  Built-in private messaging system for sellers and buyers to communicate securely.

- 🚚 **API-Based Shipping Cost Calculator** *(Coming Soon)*  
  Calculate accurate shipping costs on checkout via third-party logistics APIs.

- 📱 **Responsive UI**  
  Mobile-first design built with Bootstrap 5.

- 📊 **User Dashboards**  
  Manage listings, bids, wins, and messages all from one place.

- 🔍 **Advanced Search & Filtering**  
  Quickly locate products by keyword, category, price, and more.

- 🔐 **Admin Panel**  
  Role-based access for managing users, listings, and reports.

---

## 🛠️ Technologies Used

| Layer           | Stack                                  |
|-----------------|--------------------------------------|
| **Backend**     | ASP.NET Core MVC (.NET 8)             |
| **Frontend**    | Razor Pages + Bootstrap 5 + JavaScript (Vanilla & SignalR client) |
| **Real-Time**   | SignalR (WebSockets, powered by Redis backplane) |
| **Caching**     | Redis (distributed cache and SignalR backplane) |
| **Database**    | SQL Server + EF Core                  |
| **Authentication** | ASP.NET Identity                   |
| **Storage**     | Local file system for images          |
| **AI**          | Azure/OpenAI Vision API *(WIP)*       |
| **Shipping**    | Shipping API (e.g., EasyPost/Shippo) *(Planned)* |


## 🧠 Core Concepts

- **Product:** Item with metadata like title, category, price, description, and images.
- **Category:** Logical grouping (Electronics, Fashion, etc.).
- **Auction:** Real-time event where users place competitive bids.
- **Image Upload:** Stored in `wwwroot/images/products`, referenced by URL in DB.
- **Chat:** Real-time messaging via SignalR between users.
- **AI Description:** Vision API reads uploaded images and generates a product title + description draft.
- **Shipping:** Shipping cost dynamically calculated at checkout via API based on destination, weight, and size.


