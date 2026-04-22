# Vision AI: Image Classification and Management System

Vision AI is a comprehensive web application designed for automated image recognition and machine learning model lifecycle management. Built on the ASP.NET Core 8.0 framework and powered by ML.NET, it provides a sophisticated environment for real-time classification, performance monitoring, and model optimization.

## Project Overview

The system is engineered to bridge the gap between complex machine learning workflows and user-friendly web interfaces. It allows administrators to maintain a custom image recognition model that can be retrained on-demand as new datasets become available.

## Core Features and Capabilities

### 1. Real-Time Image Prediction
The system utilizes a trained multiclass classification model to analyze uploaded images.
- **Instant Feedback**: Results are processed and returned in milliseconds.
- **Confidence Scoring**: Each prediction includes a probability percentage, calculated using a stabilized softmax algorithm to ensure mathematical accuracy.
- **Visual Verification**: The interface displays the processed image alongside the prediction for user validation.

### 2. On-Demand Model Training Pipeline
The application features a built-in training engine that eliminates the need for external ML tools.
- **Dataset Integration**: Automatically scans the `Dataset` directory for labeled folders.
- **Fast Image Classification**: Employs the SDCA (Stochastic Dual Coordinate Ascent) maximum entropy trainer, optimized for speed and accuracy with pixel-based features.
- **Automated Preprocessing**: Includes automatic image resizing (64x64 pixels) and pixel normalization to prepare raw data for the neural network.

### 3. Performance Analytics
Every training session generates a detailed summary of the model's health:
- **Micro/Macro Accuracy**: Statistics to evaluate how well the model performs across all categories.
- **Log-Loss Metrics**: Measures the uncertainty of the predictions; lower values indicate a more confident and accurate model.
- **Duration Tracking**: Monitors the efficiency of the training process.

### 4. Security Architecture
Access to the classification engine is protected by a secure authentication layer:
- **Cookie-Based Authentication**: Ensures only authorized users can perform predictions or trigger retraining.
- **Controller-Level Authorization**: All API endpoints are guarded by the `[Authorize]` attribute.

## System Architecture

### Technology Stack
- **Framework**: ASP.NET Core 8.0 (MVC/Web API)
- **Machine Learning**: ML.NET (Microsoft.ML)
- **Database**: SQL Server (Entity Framework Core)
- **Frontend**: Razor Pages, Vanilla CSS3, Modern JavaScript (ES6+)

### Core Components
- **ImageClassificationService**: The heart of the application, managing MLContext, model loading/saving, and the training pipeline.
- **ImageClassificationController**: Exposes RESTful endpoints for prediction, training status, and results.
- **ApplicationDbContext**: Manages user authentication data and application state.

## Application Interface

### Dashboard Overview
The dashboard serves as the central control hub, providing real-time status of the ML model and access to training controls.
![Dashboard](Screenshots/Screenshot%202026-04-22%20171714.png)

### Prediction Interface
A clean, focused workspace for performing image analysis and viewing classification data.
![Prediction](Screenshots/Screenshot%202026-04-22%20171705.png)

## Installation and Technical Setup

### Prerequisites
- .NET 8.0 SDK or later
- SQL Server (LocalDB instance is recommended for development)
- Visual Studio 2022 or VS Code with C# Dev Kit

### Configuration and Deployment

1. **Clone the Repository**
   ```bash
   git clone https://github.com/yourusername/ImageClassificationAPI.git
   ```

2. **Database Setup**
   Update the connection string in `appsettings.json`.
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ImageClassificationDb;Trusted_Connection=True;"
   }
   ```

3. **Database Initialization**
   Apply migrations to create the identity schema:
   ```bash
   dotnet ef database update
   ```

4. **Prepare Training Data**
   Place your images in the `Dataset` folder, organized by label (e.g., `Dataset/Cats/`, `Dataset/Dogs/`).

5. **Run the Application**
   ```bash
   dotnet run
   ```

## API Documentation

| Endpoint | Method | Description |
| :--- | :--- | :--- |
| `/api/ImageClassification/predict` | `POST` | Uploads an image and returns the predicted label and confidence. |
| `/api/ImageClassification/train` | `POST` | Triggers the model retraining process using the current `Dataset` folder. |
| `/api/ImageClassification/summary` | `GET` | Retrieves the metrics from the most recent training session. |

---
*Developed by Hariom Lokhande*
