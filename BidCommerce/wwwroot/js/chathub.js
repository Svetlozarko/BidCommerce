// Fixed SignalR Chat Connection
import * as signalR from "@microsoft/signalr"

class ChatConnection {
    constructor() {
        this.connection = null
        this.isConnecting = false
        this.messageQueue = []
        this.maxRetries = 5
        this.retryCount = 0
        this.retryDelay = 1000
    }

    async initialize() {
        if (this.connection) {
            await this.disconnect()
        }

        try {
            // Create new connection
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl("/chathub", {
                    transport: signalR.HttpTransportType.WebSockets,
                    skipNegotiation: true,
                })
                .withAutomaticReconnect([0, 2000, 10000, 30000])
                .configureLogging(signalR.LogLevel.Information)
                .build()

            // Set up event handlers
            this.setupEventHandlers()

            // Connect
            await this.connect()

            console.log("SignalR connection initialized successfully")
            return true
        } catch (error) {
            console.error("Failed to initialize SignalR connection:", error)
            return false
        }
    }

    setupEventHandlers() {
        // Connection events
        this.connection.onclose((error) => {
            console.log("SignalR connection closed:", error)
            this.handleConnectionClosed()
        })

        this.connection.onreconnecting((error) => {
            console.log("SignalR reconnecting:", error)
            this.updateConnectionStatus("Reconnecting...")
        })

        this.connection.onreconnected((connectionId) => {
            console.log("SignalR reconnected:", connectionId)
            this.updateConnectionStatus("Connected")
            this.retryCount = 0
            this.processMessageQueue()
        })

        // Message events
        this.connection.on("ReceiveMessage", (senderId, message) => {
            this.displayMessage(senderId, message)
        })

        this.connection.on("ReceiveMessageHistory", (messages) => {
            this.displayMessageHistory(messages)
        })

        this.connection.on("MessageError", (error) => {
            console.error("Message error:", error)
            this.showError(error)
        })
    }

    async connect() {
        if (this.isConnecting) {
            console.log("Connection already in progress")
            return
        }

        this.isConnecting = true
        this.updateConnectionStatus("Connecting...")

        try {
            await this.connection.start()
            console.log("SignalR connected successfully")
            this.updateConnectionStatus("Connected")
            this.retryCount = 0
            this.processMessageQueue()
            return true
        } catch (error) {
            console.error("SignalR connection failed:", error)
            this.updateConnectionStatus("Disconnected")
            await this.handleConnectionError(error)
            return false
        } finally {
            this.isConnecting = false
        }
    }

    async disconnect() {
        if (this.connection) {
            try {
                await this.connection.stop()
                console.log("SignalR disconnected")
            } catch (error) {
                console.error("Error disconnecting:", error)
            }
        }
    }

    async sendMessage(receiverId, message) {
        if (!receiverId || !message.trim()) {
            this.showError("Please enter a message and select a recipient")
            return false
        }

        // Check connection state
        if (!this.isConnected()) {
            console.log("Connection not ready, queueing message")
            this.queueMessage("SendMessage", [receiverId, message.trim()])
            await this.ensureConnection()
            return false
        }

        try {
            await this.connection.invoke("SendMessage", receiverId, message.trim())
            console.log("Message sent successfully")
            return true
        } catch (error) {
            console.error("Failed to send message:", error)
            this.showError("Failed to send message: " + error.message)

            // Queue message for retry if connection issue
            if (this.isConnectionError(error)) {
                this.queueMessage("SendMessage", [receiverId, message.trim()])
                await this.ensureConnection()
            }

            return false
        }
    }

    async getMessageHistory(receiverId) {
        if (!this.isConnected()) {
            await this.ensureConnection()
            if (!this.isConnected()) {
                this.showError("Cannot load message history - connection failed")
                return
            }
        }

        try {
            await this.connection.invoke("GetMessageHistory", receiverId)
        } catch (error) {
            console.error("Failed to get message history:", error)
            this.showError("Failed to load message history")
        }
    }

    isConnected() {
        return this.connection && this.connection.state === signalR.HubConnectionState.Connected
    }

    isConnectionError(error) {
        const connectionErrors = [
            "Cannot send data if the connection is not in the 'Connected' State",
            "Connection disconnected",
            "Connection closed",
            "WebSocket connection failed",
        ]
        return connectionErrors.some((err) => error.message.includes(err))
    }

    queueMessage(method, args) {
        this.messageQueue.push({ method, args, timestamp: Date.now() })
        console.log(`Queued message: ${method}`, args)
    }

    async processMessageQueue() {
        if (!this.isConnected() || this.messageQueue.length === 0) {
            return
        }

        console.log(`Processing ${this.messageQueue.length} queued messages`)

        const queue = [...this.messageQueue]
        this.messageQueue = []

        for (const item of queue) {
            try {
                await this.connection.invoke(item.method, ...item.args)
                console.log(`Processed queued message: ${item.method}`)
            } catch (error) {
                console.error(`Failed to process queued message: ${item.method}`, error)
                // Re-queue if it's a connection error
                if (this.isConnectionError(error)) {
                    this.messageQueue.push(item)
                }
            }
        }
    }

    async ensureConnection() {
        if (this.isConnected()) {
            return true
        }

        if (this.isConnecting) {
            // Wait for current connection attempt
            await this.waitForConnection()
            return this.isConnected()
        }

        return await this.connect()
    }

    async waitForConnection(timeout = 5000) {
        const start = Date.now()
        while (this.isConnecting && Date.now() - start < timeout) {
            await new Promise((resolve) => setTimeout(resolve, 100))
        }
    }

    async handleConnectionError(error) {
        if (this.retryCount < this.maxRetries) {
            this.retryCount++
            const delay = this.retryDelay * Math.pow(2, this.retryCount - 1)

            console.log(`Retrying connection in ${delay}ms (attempt ${this.retryCount}/${this.maxRetries})`)
            this.updateConnectionStatus(`Retrying in ${delay / 1000}s...`)

            setTimeout(() => {
                this.connect()
            }, delay)
        } else {
            console.error("Max retry attempts reached")
            this.updateConnectionStatus("Connection failed")
            this.showError("Unable to connect to chat server. Please refresh the page.")
        }
    }

    handleConnectionClosed() {
        this.updateConnectionStatus("Disconnected")
        if (this.retryCount < this.maxRetries) {
            setTimeout(() => {
                this.connect()
            }, 2000)
        }
    }

    updateConnectionStatus(status) {
        const statusElement = document.getElementById("connectionStatus")
        if (statusElement) {
            statusElement.textContent = status
            statusElement.className = `connection-status ${status.toLowerCase().replace(/\s+/g, "-")}`
        }
        console.log("Connection status:", status)
    }

    displayMessage(senderId, message) {
        const messagesContainer = document.getElementById("messagesContainer")
        if (!messagesContainer) return

        const messageElement = document.createElement("div")
        messageElement.className = "message"
        messageElement.innerHTML = `
      <div class="message-sender">${senderId}</div>
      <div class="message-content">${this.escapeHtml(message)}</div>
      <div class="message-time">${new Date().toLocaleTimeString()}</div>
    `

        messagesContainer.appendChild(messageElement)
        messagesContainer.scrollTop = messagesContainer.scrollHeight
    }

    displayMessageHistory(messages) {
        const messagesContainer = document.getElementById("messagesContainer")
        if (!messagesContainer) return

        messagesContainer.innerHTML = ""

        messages.forEach((msg) => {
            const messageElement = document.createElement("div")
            messageElement.className = "message"
            messageElement.innerHTML = `
        <div class="message-sender">${msg.senderId}</div>
        <div class="message-content">${this.escapeHtml(msg.content)}</div>
        <div class="message-time">${new Date(msg.sentAt).toLocaleTimeString()}</div>
      `
            messagesContainer.appendChild(messageElement)
        })

        messagesContainer.scrollTop = messagesContainer.scrollHeight
    }

    showError(message) {
        const errorElement = document.getElementById("errorMessage")
        if (errorElement) {
            errorElement.textContent = message
            errorElement.style.display = "block"
            setTimeout(() => {
                errorElement.style.display = "none"
            }, 5000)
        }
        console.error("Chat error:", message)
    }

    escapeHtml(text) {
        const div = document.createElement("div")
        div.textContent = text
        return div.innerHTML
    }
}

// Global chat instance
let chatConnection = null

// Initialize when DOM is ready
document.addEventListener("DOMContentLoaded", async () => {
    console.log("Initializing chat connection...")

    chatConnection = new ChatConnection()
    const success = await chatConnection.initialize()

    if (!success) {
        console.error("Failed to initialize chat connection")
        return
    }

    // Set up UI event handlers
    setupChatUI()
})

function setupChatUI() {
    // Send message on Enter key or button click
    const messageInput = document.getElementById("messageInput")
    const sendButton = document.getElementById("sendButton")

    if (messageInput) {
        messageInput.addEventListener("keypress", async (e) => {
            if (e.key === "Enter" && !e.shiftKey) {
                e.preventDefault()
                await sendMessage()
            }
        })
    }

    if (sendButton) {
        sendButton.addEventListener("click", sendMessage)
    }
}

async function sendMessage() {
    const messageInput = document.getElementById("messageInput")
    const receiverSelect = document.getElementById("receiverSelect")

    if (!messageInput || !receiverSelect) {
        console.error("Message input or receiver select not found")
        return
    }

    const message = messageInput.value.trim()
    const receiverId = receiverSelect.value

    if (!message || !receiverId) {
        chatConnection.showError("Please enter a message and select a recipient")
        return
    }

    const success = await chatConnection.sendMessage(receiverId, message)
    if (success) {
        messageInput.value = ""
    }
}

async function loadMessageHistory(receiverId) {
    if (chatConnection && receiverId) {
        await chatConnection.getMessageHistory(receiverId)
    }
}

// Make functions globally available
window.sendMessage = sendMessage
window.loadMessageHistory = loadMessageHistory
