# .NET MAUI Local Storage Profile Assignment

## 📌 Overview
This project is a single-page .NET MAUI application that demonstrates **local storage** by creating and managing a user profile. The profile data is stored in JSON format inside a local text file, ensuring persistence across app sessions.

---

## 🛠 Features
- Profile Page with editable fields:
  - Name
  - Surname
  - Email Address
  - Bio
- Local storage using JSON format
- Data persistence:
  - Pre-populates fields with saved data when the app opens
  - Updates and saves changes on **Save Button** click
- Bonus Challenge:
  - Add a profile icon
  - Add and save a picture with the profile

---

## 📂 Project Structure
- **MainPage.xaml / MainPage.xaml.cs** → UI and logic for the Profile Page
- **Models/Profile.cs** → Profile data model
- **Services/ProfileStorage.cs** → Handles JSON serialization and local file storage
- **profile.json** → Local text file storing user profile data

---

## 🚀 How It Works
1. On app launch, the Profile Page checks for `profile.json` in local storage.
2. If found, it loads and displays the saved profile data.
3. The user can edit fields and click **Save**.
4. The updated profile is serialized into JSON and written back to `profile.json`.

