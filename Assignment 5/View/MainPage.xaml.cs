using AssiGnment_5.Models;
using AssiGnment_5.Services;
using Microsoft.Maui.Storage;

namespace AssiGnment_5.View
{
    public partial class MainPage : ContentPage
    {
        private readonly SupabaseService _supabase;
        private Guid _userId;
        private string _currentImageUrl = string.Empty;
        private string _localImagePath = string.Empty;

        public MainPage()
        {
            InitializeComponent();

            // Set default IMMEDIATELY after InitializeComponent — first thing rendered
            ProfileImage.Source = "profiledefault.png";

            _supabase = new SupabaseService();

            // Retrieve or create a persistent user ID
            var stored = Preferences.Get("UserId", string.Empty);
            if (!string.IsNullOrEmpty(stored))
                _userId = Guid.Parse(stored);
            else
            {
                _userId = Guid.NewGuid();
                Preferences.Set("UserId", _userId.ToString());
            }

            // Restore local avatar instantly (no network) if one was saved before
            string savedLocal = Preferences.Get("LocalAvatarPath", string.Empty);
            if (!string.IsNullOrEmpty(savedLocal) && File.Exists(savedLocal))
            {
                _localImagePath = savedLocal;
                ProfileImage.Source = ImageSource.FromFile(savedLocal);
            }
            // Otherwise default stays — no async, no blank flash
        }

        // ─── PAGE LIFECYCLE ──────────────────────────────────────────────────────

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Guarantee image is visible before any network call
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

                // Always restore image right after the await — it may have blanked
                RestoreImage();

                if (profile != null)
                {
                    NameEntry.Text = profile.Name;
                    SurnameEntry.Text = profile.Surname;
                    EmailEntry.Text = profile.EmailAddress;
                    BioEditor.Text = profile.Bio;

                    if (!string.IsNullOrEmpty(profile.ProfileIconPath))
                    {
                        _currentImageUrl = profile.ProfileIconPath;

                        // Prefer local copy — fast and no network flicker
                        if (!string.IsNullOrEmpty(_localImagePath) && File.Exists(_localImagePath))
                            ProfileImage.Source = ImageSource.FromFile(_localImagePath);
                        else
                            ApplyRemoteImage(_currentImageUrl);
                    }
                    // profile exists but no image → RestoreImage() already set default above
                }
                // no profile → RestoreImage() already set default above
            }
            catch
            {
                // Network error → keep whatever is showing (default or local)
                RestoreImage();
            }
        }

        // ─── SAVE ────────────────────────────────────────────────────────────────

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            try
            {
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
                await DisplayAlert("Success", "Profile saved!", "OK");
                RestoreImage();
            }
            catch (Exception ex)
            {
                RestoreImage();
                await DisplayAlert("Save Error", ex.Message, "OK");
                RestoreImage();
            }
        }

        // ─── PROFILE PICTURE ─────────────────────────────────────────────────────

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

                // Save permanent local copy named by userId so it survives reinstall data
                string fileName = $"avatar_{_userId}{Path.GetExtension(result.FullPath)}";
                string localPath = Path.Combine(FileSystem.AppDataDirectory, fileName);
                File.Copy(result.FullPath, localPath, true);

                _localImagePath = localPath;
                Preferences.Set("LocalAvatarPath", localPath);

                // Show local file immediately — rock solid, no network dependency
                ProfileImage.Source = ImageSource.FromFile(localPath);

                // Upload to Supabase
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

        // ─── HELPERS ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Restores the best available image in priority order.
        /// NEVER leaves the image blank — always falls back to default.
        /// </summary>
        private void RestoreImage()
        {
            // 1. In-memory local path (set this session)
            if (!string.IsNullOrEmpty(_localImagePath) && File.Exists(_localImagePath))
            {
                ProfileImage.Source = ImageSource.FromFile(_localImagePath);
                return;
            }

            // 2. Saved local path from previous session
            string saved = Preferences.Get("LocalAvatarPath", string.Empty);
            if (!string.IsNullOrEmpty(saved) && File.Exists(saved))
            {
                _localImagePath = saved;
                ProfileImage.Source = ImageSource.FromFile(saved);
                return;
            }

            // 3. Remote URL from Supabase
            if (!string.IsNullOrEmpty(_currentImageUrl))
            {
                ApplyRemoteImage(_currentImageUrl);
                return;
            }

            // 4. Default — always works, no network needed
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