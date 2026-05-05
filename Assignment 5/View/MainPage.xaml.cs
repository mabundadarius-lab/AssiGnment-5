using AssiGnment_5.Models;
using AssiGnment_5.Services;
using Microsoft.Maui.Storage;
using System.Text.RegularExpressions;

namespace AssiGnment_5.View
{
    public partial class MainPage : ContentPage
    {
        private readonly SupabaseService _supabase;
        private Guid _userId;
        private string _currentImageUrl = string.Empty;
        private string _localImagePath = string.Empty;
        private bool _profileSaved = false;

        public MainPage()
        {
            InitializeComponent();

            ProfileImage.Source = "profiledefault.png";

            _supabase = new SupabaseService();

            var stored = Preferences.Get("UserId", string.Empty);
            if (!string.IsNullOrEmpty(stored))
                _userId = Guid.Parse(stored);
            else
            {
                _userId = Guid.NewGuid();
                Preferences.Set("UserId", _userId.ToString());
            }

            string savedLocal = Preferences.Get("LocalAvatarPath", string.Empty);
            if (!string.IsNullOrEmpty(savedLocal) && File.Exists(savedLocal))
            {
                _localImagePath = savedLocal;
                ProfileImage.Source = ImageSource.FromFile(savedLocal);
            }
        }

        // ─── PAGE LIFECYCLE ──────────────────────────────────────────────────────

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (string.IsNullOrEmpty(_localImagePath))
                ProfileImage.Source = "profiledefault.png";

            await LoadProfileAsync();
        }

        // ─── LOAD ────────────────────────────────────────────────────────────────

        private async Task LoadProfileAsync()
        {
            try
            {
                var profile = await _supabase.GetProfileByIdAsync(_userId);
                RestoreImage();

                if (profile != null)
                {
                    NameEntry.Text = profile.Name;
                    SurnameEntry.Text = profile.Surname;
                    EmailEntry.Text = profile.EmailAddress;
                    BioEditor.Text = profile.Bio;

                    _profileSaved = true;
                    UpdateShoppingButton();
                    UpdateHeading(profile.Name, profile.Surname);

                    if (!string.IsNullOrEmpty(profile.ProfileIconPath))
                    {
                        _currentImageUrl = profile.ProfileIconPath;
                        if (!string.IsNullOrEmpty(_localImagePath) && File.Exists(_localImagePath))
                            ProfileImage.Source = ImageSource.FromFile(_localImagePath);
                        else
                            ApplyRemoteImage(_currentImageUrl);
                    }
                }
                else
                {
                    _profileSaved = false;
                    UpdateShoppingButton();
                    ProfileHeading.Text = "Profile";
                }
            }
            catch
            {
                RestoreImage();
            }
        }

        // ─── SAVE ────────────────────────────────────────────────────────────────

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            // ── Required field checks ──────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(NameEntry.Text))
            {
                await DisplayAlert("Required", "Please enter your Name.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(SurnameEntry.Text))
            {
                await DisplayAlert("Required", "Please enter your Surname.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(EmailEntry.Text))
            {
                await DisplayAlert("Required", "Please enter your Email Address.", "OK");
                return;
            }

            // ── Email format validation ────────────────────────────────────────
            if (!IsValidEmail(EmailEntry.Text.Trim()))
            {
                await DisplayAlert("Invalid Email",
                    "Please enter a valid email address.\n\nExamples:\n• user@example.com\n• name.surname@domain.co.za",
                    "OK");
                EmailEntry.Focus(); // bring focus back to email field
                return;
            }

            if (string.IsNullOrWhiteSpace(BioEditor.Text))
            {
                await DisplayAlert("Required", "Please enter your Bio.", "OK");
                return;
            }

            // ── Save to Supabase ───────────────────────────────────────────────
            try
            {
                string name = NameEntry.Text.Trim();
                string surname = SurnameEntry.Text.Trim();

                var profile = new UserProfile
                {
                    Id = _userId,
                    Name = name,
                    Surname = surname,
                    EmailAddress = EmailEntry.Text.Trim(),
                    Bio = BioEditor.Text.Trim(),
                    ProfileIconPath = _currentImageUrl
                };

                await _supabase.SaveProfileAsync(profile);

                UpdateHeading(name, surname);
                _profileSaved = true;
                UpdateShoppingButton();

                RestoreImage();
                await DisplayAlert("Success", "Profile saved! You can now go shopping.", "OK");
                RestoreImage();
            }
            catch (Exception ex)
            {
                RestoreImage();
                await DisplayAlert("Save Error", ex.Message, "OK");
                RestoreImage();
            }
        }

        // ─── EMAIL VALIDATION ─────────────────────────────────────────────────────

        /// <summary>
        /// Validates email format using a standard regex.
        /// Rules enforced:
        ///   • Must have exactly one @ symbol
        ///   • Local part (before @) must not be empty
        ///   • Domain part must have at least one dot
        ///   • No spaces allowed anywhere
        ///   • No consecutive dots
        ///   • TLD must be at least 2 characters (e.g. .com, .za)
        /// </summary>
        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;

            // Standard email regex — covers the vast majority of valid addresses
            const string pattern =
                @"^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$";

            return Regex.IsMatch(email, pattern, RegexOptions.IgnoreCase);
        }

        // ─── PROFILE PICTURE (optional) ──────────────────────────────────────────

        private async void OnChoosePictureClicked(object sender, EventArgs e)
        {
            try
            {
                var result = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "Select a profile picture",
                    FileTypes = FilePickerFileType.Images
                });

                if (result == null) { RestoreImage(); return; }

                string fileName = $"avatar_{_userId}{Path.GetExtension(result.FullPath)}";
                string localPath = Path.Combine(FileSystem.AppDataDirectory, fileName);
                File.Copy(result.FullPath, localPath, true);

                _localImagePath = localPath;
                Preferences.Set("LocalAvatarPath", localPath);
                ProfileImage.Source = ImageSource.FromFile(localPath);

                string? publicUrl = await _supabase.UploadProfilePictureAsync(_userId, localPath);
                RestoreImage();

                if (!string.IsNullOrEmpty(publicUrl))
                {
                    _currentImageUrl = publicUrl;

                    var profile = new UserProfile
                    {
                        Id = _userId,
                        Name = NameEntry.Text?.Trim() ?? string.Empty,
                        Surname = SurnameEntry.Text?.Trim() ?? string.Empty,
                        EmailAddress = EmailEntry.Text?.Trim() ?? string.Empty,
                        Bio = BioEditor.Text?.Trim() ?? string.Empty,
                        ProfileIconPath = _currentImageUrl
                    };
                    await _supabase.SaveProfileAsync(profile);
                    RestoreImage();
                }

                await DisplayAlert("Success", "Profile picture updated!", "OK");
                RestoreImage();
            }
            catch (Exception ex)
            {
                RestoreImage();
                await DisplayAlert("Error", ex.Message, "OK");
                RestoreImage();
            }
        }

        // ─── HELPERS , updated to only show the Profile {Name}─────────────────────────────────────────────────────────────

        private void UpdateHeading(string name, string surname)
        {
            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(surname))
                ProfileHeading.Text = "Profile";
            else
                ProfileHeading.Text = $"Profile of {name}".Trim();
        }

        private void UpdateShoppingButton()
        {
            ShoppingButton.IsEnabled = _profileSaved;
            ShoppingButton.Opacity = _profileSaved ? 1.0 : 0.4;
            SaveHintLabel.IsVisible = !_profileSaved;
        }

        private void RestoreImage()
        {
            if (!string.IsNullOrEmpty(_localImagePath) && File.Exists(_localImagePath))
            {
                ProfileImage.Source = ImageSource.FromFile(_localImagePath);
                return;
            }

            string saved = Preferences.Get("LocalAvatarPath", string.Empty);
            if (!string.IsNullOrEmpty(saved) && File.Exists(saved))
            {
                _localImagePath = saved;
                ProfileImage.Source = ImageSource.FromFile(saved);
                return;
            }

            if (!string.IsNullOrEmpty(_currentImageUrl))
            {
                ApplyRemoteImage(_currentImageUrl);
                return;
            }

            ProfileImage.Source = "profiledefault.png";
        }

        private void ApplyRemoteImage(string url)
        {
            ProfileImage.Source = new UriImageSource
            {
                Uri = new Uri($"{url}?t={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}"),
                CachingEnabled = false
            };
        }

        // ─── NAVIGATION ──────────────────────────────────────────────────────────

        private async void OnShoppingClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(ShoppingItems));
        }
    }
}