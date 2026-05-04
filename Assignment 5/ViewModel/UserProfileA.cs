using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using AssiGnment_5.Models;
using AssiGnment_5.Services;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls;

namespace AssiGnment_5.ViewModel
{
    internal class UserProfileViewModel: BaseViewModel
    {
        private readonly SupabaseService _supabase;
        private UserProfile _profile;

        public string Name { get; set; }
        public string Surname { get; set; }
        public string Email { get; set; }
        public string Bio { get; set; }

        private string _profileIconPath;
        public string ProfileIconPath
        {
            get => _profileIconPath;
            set { _profileIconPath = value; OnPropertyChanged(); }
        }

        public ICommand ChangePictureCommand { get; }
        public ICommand SaveProfileCommand { get; }

        public UserProfileViewModel()
        {
            _supabase = new SupabaseService();
            ChangePictureCommand = new Command(async () => await ChangeProfilePictureAsync());
            SaveProfileCommand = new Command(async () => await SaveProfileAsync());
        }

        private async Task ChangeProfilePictureAsync()
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

                // Update property so UI refreshes
                ProfileIconPath = localImagePath;

                // Save immediately to Supabase
                if (_profile == null) _profile = new UserProfile();
                _profile.ProfileIconPath = localImagePath;
                await _supabase.SaveProfileAsync(_profile);
            }
        }

        private async Task SaveProfileAsync()
        {
            if (_profile == null) _profile = new UserProfile();
            _profile.Name = Name;
            _profile.Surname = Surname;
            _profile.EmailAddress = Email;
            _profile.Bio = Bio;
            _profile.ProfileIconPath = ProfileIconPath;
            await _supabase.SaveProfileAsync(_profile);
        }
    }
}
