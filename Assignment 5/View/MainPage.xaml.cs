using System.Text.Json;
using AssiGnment_5.ViewModel;

namespace AssiGnment_5
{
    public partial class MainPage : ContentPage
    {
        private readonly string filePath = Path.Combine(FileSystem.AppDataDirectory, "profile.json");

        public MainPage()
        {
            InitializeComponent();
            LoadProfile(); // Load saved profile when app starts
        }

        // Load profile data from JSON file
        private void LoadProfile()
        {
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                var profile = JsonSerializer.Deserialize<UserProfile>(json);

                NameEntry.Text = profile.Name;
                SurnameEntry.Text = profile.Surname;
                EmailEntry.Text = profile.EmailAddress;
                BioEditor.Text = profile.Bio;

                // Reload saved picture from local storage
                if (!string.IsNullOrEmpty(profile.ProfileIconPath))
                    ProfileImage.Source = ImageSource.FromFile(profile.ProfileIconPath);
            }
        }

        // Save profile data to JSON file
        private void OnSaveClicked(object sender, EventArgs e)
        {
            var profile = new UserProfile
            {
                Name = NameEntry.Text,
                Surname = SurnameEntry.Text,
                EmailAddress = EmailEntry.Text,
                Bio = BioEditor.Text,
                ProfileIconPath = (ProfileImage.Source as FileImageSource)?.File
            };

            string json = JsonSerializer.Serialize(profile);
            File.WriteAllText(filePath, json);

            DisplayAlert("Success", "Profile saved!", "OK");
        }

        // Allow user to choose a profile picture
        private async void OnChoosePictureClicked(object sender, EventArgs e)
        {
            var result = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Select a profile picture",
                FileTypes = FilePickerFileType.Images
            });

            if (result != null)
            {
                // Copy chosen image into app's local storage
                string localImagePath = Path.Combine(FileSystem.AppDataDirectory, Path.GetFileName(result.FullPath));
                File.Copy(result.FullPath, localImagePath, true);

                // Show chosen picture
                ProfileImage.Source = ImageSource.FromFile(localImagePath);

                // Save immediately so it persists next run
                var profile = new UserProfile
                {
                    Name = NameEntry.Text,
                    Surname = SurnameEntry.Text,
                    EmailAddress = EmailEntry.Text,
                    Bio = BioEditor.Text,
                    ProfileIconPath = localImagePath // Always point to local copy
                };

                string json = JsonSerializer.Serialize(profile);
                File.WriteAllText(filePath, json);
            }
        }
    }
}





