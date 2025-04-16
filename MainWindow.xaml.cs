using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using ManajemenBarang.Models;
using ManajemenBarang.Services;
using System.Globalization;
using Microsoft.Win32;
using System.IO;
using System.Text.Json;
using System.Configuration;
using System.Data;
using System.Threading.Tasks;
using WinForms = System.Windows.Forms;
using Win32 = Microsoft.Win32;
using MsgBox = System.Windows.MessageBox;
using ManajemenBarang.Views;
using System.Diagnostics;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace ManajemenBarang;

// Extension methods
public static class Extensions
{
    public static string Repeat(this string text, int count)
    {
        return string.Concat(Enumerable.Repeat(text, count));
    }
}

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly StorageService _storageService;
    private List<Item> _daftarBarang = new();
    private Item? _selectedItem;
    private bool _isEditMode = false;
    private string _backupFolderPath;
    private string? _currentFotoPath;
    private string? _currentKondisiAwalPath;
    private string? _currentKondisiAkhirPath;
    private string? _selectedImagePath;
    private string? _selectedKondisiAwalPath;
    private string? _selectedKondisiAkhirPath;
    private string? _currentEditingId;

    public MainWindow()
    {
        InitializeComponent();
        _storageService = new StorageService();

        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _backupFolderPath = Path.Combine(appDataPath, "ManajemenBarang", "Backups");
        Directory.CreateDirectory(_backupFolderPath);

        _ = LoadData();
        ClearInputs();
        
        // Menonaktifkan tombol pilih foto saat pertama kali
        btnPilihFoto.IsEnabled = false;
        btnPilihFotoKondisiAwal.IsEnabled = false;
        btnPilihFotoKondisiAkhir.IsEnabled = false;
        
        dpTanggalPerolehan.SelectedDate = DateTime.Now;
        dpTanggalPinjam.SelectedDate = DateTime.Now;
        
        // Connect TabControl selection changed event
        tabControl.SelectionChanged += TabControl_SelectionChanged;
    }

    private async Task LoadData()
    {
        try
        {
            _daftarBarang = await _storageService.GetItemsAsync();
            
            // Debug: Print foto paths
            foreach (var item in _daftarBarang)
            {
                System.Diagnostics.Debug.WriteLine($"Item {item.IdBarang} foto path: {item.FotoPath}");
                if (!string.IsNullOrEmpty(item.FotoPath))
                {
                    string fullPath = Path.Combine(_storageService.ImagePath, item.FotoPath);
                    System.Diagnostics.Debug.WriteLine($"Full path: {fullPath}, Exists: {File.Exists(fullPath)}");
                }
            }
            
            dgBarang.ItemsSource = null;
            dgBarang.ItemsSource = _daftarBarang;
        }
        catch (Exception ex)
        {
            MsgBox.Show($"Terjadi kesalahan saat memuat data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private int GetNextId()
    {
        if (_daftarBarang.Count == 0)
            return 1;
        
        return _daftarBarang.Select(item => int.Parse(item.IdBarang)).OrderBy(id => id).Last() + 1;
    }

    private void ClearInputs()
    {
        // Reset all text inputs
        txtIdBarang.Text = string.Empty;
        txtNamaBarang.Text = string.Empty;
        txtMerekBarang.Text = string.Empty;
        txtHargaBarang.Text = string.Empty;
        dpTanggalPerolehan.SelectedDate = DateTime.Now;
        dpTanggalPinjam.SelectedDate = DateTime.Now;
        txtNamaPengguna.Text = string.Empty;
        txtBidang.Text = string.Empty;
        txtKeteranganBarang.Text = string.Empty;
        
        // Clear image paths
        _selectedImagePath = null;
        _selectedKondisiAwalPath = null;
        _selectedKondisiAkhirPath = null;
        _currentFotoPath = null;
        _currentKondisiAwalPath = null;
        _currentKondisiAkhirPath = null;
        
        // Reset image controls
        imgBarang.Source = null;
        imgKondisiAwal.Source = null;
        imgKondisiAkhir.Source = null;
        noImagePlaceholder.Visibility = Visibility.Visible;
        noImagePlaceholderKondisiAwal.Visibility = Visibility.Visible;
        noImagePlaceholderKondisiAkhir.Visibility = Visibility.Visible;
        
        // Reset state flags
        _isEditMode = false;
        _currentEditingId = null;
        _selectedItem = null;
        
        // Enable ID field for new items
        txtIdBarang.IsEnabled = true;
        
        // Make sure photo buttons are enabled
        btnPilihFoto.IsEnabled = true;
        btnPilihFotoKondisiAwal.IsEnabled = true;
        btnPilihFotoKondisiAkhir.IsEnabled = true;
        
        // Reset DataGrid selection
        if (dgBarang.SelectedItem != null)
        {
            dgBarang.UnselectAll();
        }
        
        txtNamaBarang.Focus();
    }

    private bool ValidateInputs()
    {
        if (string.IsNullOrWhiteSpace(txtIdBarang.Text))
        {
            MsgBox.Show("No ID Barang harus diisi!", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
            txtIdBarang.Focus();
            return false;
        }

        if (!int.TryParse(txtIdBarang.Text, out _))
        {
            MsgBox.Show("No ID Barang harus berupa angka!", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
            txtIdBarang.Focus();
            return false;
        }
        
        // Strong duplicate ID check - always check regardless of edit mode
        string currentId = txtIdBarang.Text.Trim();
        bool isDuplicate = false;
        
        if (_isEditMode)
        {
            // In edit mode, check if the ID has changed and if it conflicts with another item
            if (_currentEditingId != currentId)
            {
                isDuplicate = _daftarBarang.Any(item => item.IdBarang == currentId);
                Debug.WriteLine($"Edit mode - ID changed from {_currentEditingId} to {currentId}, isDuplicate: {isDuplicate}");
            }
        }
        else
        {
            // In add mode, always check for duplicates
            isDuplicate = _daftarBarang.Any(item => item.IdBarang == currentId);
            Debug.WriteLine($"Add mode - checking ID {currentId}, isDuplicate: {isDuplicate}");
        }
        
        if (isDuplicate)
        {
            MsgBox.Show($"ID Barang '{currentId}' sudah digunakan. Harap gunakan ID lain.", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
            txtIdBarang.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(txtNamaBarang.Text))
        {
            MsgBox.Show("Nama Barang harus diisi!", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
            txtNamaBarang.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(txtMerekBarang.Text))
        {
            MsgBox.Show("Merek Barang harus diisi!", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
            txtMerekBarang.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(txtHargaBarang.Text))
        {
            MsgBox.Show("Harga Barang harus diisi!", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
            txtHargaBarang.Focus();
            return false;
        }
        
        if (!decimal.TryParse(txtHargaBarang.Text.Replace("Rp", "").Replace(".", "").Replace(",", "").Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out _))
        {
            MsgBox.Show("Harga Barang harus diisi dengan angka yang valid!", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
            txtHargaBarang.Focus();
            return false;
        }

        if (dpTanggalPerolehan.SelectedDate == null)
        {
            MsgBox.Show("Tanggal Perolehan harus dipilih!", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
            dpTanggalPerolehan.Focus();
            return false;
        }

        if (dpTanggalPinjam.SelectedDate == null)
        {
            MsgBox.Show("Tanggal Pinjam harus dipilih!", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
            dpTanggalPinjam.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(txtNamaPengguna.Text))
        {
            MsgBox.Show("Nama Pengguna harus diisi!", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
            txtNamaPengguna.Focus();
            return false;
        }

        return true;
    }

    private Item GetItemFromInputs()
    {
        // Validasi input
        if (!ValidateInputs())
        {
            return null;
        }

        // Ambil data dari input
        var idBarang = txtIdBarang.Text.Trim();
        var namaBarang = txtNamaBarang.Text.Trim();
        var merekBarang = txtMerekBarang.Text.Trim();
        var hargaBarang = decimal.Parse(txtHargaBarang.Text.Replace(".", "").Replace(",", "").Replace("Rp", "").Trim());
        var tanggalPembelian = dpTanggalPerolehan.SelectedDate;
        var tanggalPinjam = dpTanggalPinjam.SelectedDate;
        var namaPengguna = txtNamaPengguna.Text.Trim();
        var bidang = txtBidang.Text.Trim();
        var keteranganBarang = txtKeteranganBarang.Text.Trim();

        Debug.WriteLine($"Creating item with ID: {idBarang}");

        // Buat objek barang baru
        var item = new Item
        {
            IdBarang = idBarang,
            NamaBarang = namaBarang,
            MerekBarang = merekBarang,
            HargaBarang = hargaBarang,
            TanggalPerolehan = tanggalPembelian ?? DateTime.Now,
            TanggalPinjam = tanggalPinjam ?? DateTime.Now,
            NamaPengguna = namaPengguna,
            KeteranganBarang = keteranganBarang,
            Bidang = bidang
        };

        // Jika edit mode, gunakan foto yang sudah ada jika tidak ada pilihan foto baru
        if (_isEditMode && _selectedItem != null)
        {
            // Use the original ID when saving images in edit mode
            string originalId = _currentEditingId ?? idBarang;
            Debug.WriteLine($"Edit mode: Using original ID for images: {originalId}");
            
            // Untuk foto utama
            if (_selectedImagePath != null)
            {
                Debug.WriteLine($"Saving new main photo from: {_selectedImagePath}");
                // SaveImage returns the fileName, not a FileInfo
                var fileName = _storageService.SaveImage(_selectedImagePath, originalId, "foto");
                item.FotoPath = fileName;
                Debug.WriteLine($"Saved main photo as: {fileName}");
            }
            else
            {
                item.FotoPath = _selectedItem.FotoPath;
                Debug.WriteLine($"Keeping existing main photo: {item.FotoPath}");
            }

            // Untuk foto kondisi awal
            if (_selectedKondisiAwalPath != null)
            {
                Debug.WriteLine($"Saving new kondisi awal photo from: {_selectedKondisiAwalPath}");
                var fileName = _storageService.SaveImage(_selectedKondisiAwalPath, originalId, "kondisi_awal");
                item.KondisiAwalPath = fileName;
                Debug.WriteLine($"Saved kondisi awal photo as: {fileName}");
            }
            else
            {
                item.KondisiAwalPath = _selectedItem.KondisiAwalPath;
                Debug.WriteLine($"Keeping existing kondisi awal photo: {item.KondisiAwalPath}");
            }

            // Untuk foto kondisi akhir
            if (_selectedKondisiAkhirPath != null)
            {
                Debug.WriteLine($"Saving new kondisi akhir photo from: {_selectedKondisiAkhirPath}");
                var fileName = _storageService.SaveImage(_selectedKondisiAkhirPath, originalId, "kondisi_akhir");
                item.KondisiAkhirPath = fileName;
                Debug.WriteLine($"Saved kondisi akhir photo as: {fileName}");
            }
            else
            {
                item.KondisiAkhirPath = _selectedItem.KondisiAkhirPath;
                Debug.WriteLine($"Keeping existing kondisi akhir photo: {item.KondisiAkhirPath}");
            }
        }
        else
        {
            Debug.WriteLine("Add mode: Using current ID for images");
            // Mode tambah baru
            if (_selectedImagePath != null)
            {
                Debug.WriteLine($"Saving new main photo from: {_selectedImagePath}");
                var fileName = _storageService.SaveImage(_selectedImagePath, idBarang, "foto");
                item.FotoPath = fileName;
                Debug.WriteLine($"Saved main photo as: {fileName}");
            }

            if (_selectedKondisiAwalPath != null)
            {
                Debug.WriteLine($"Saving new kondisi awal photo from: {_selectedKondisiAwalPath}");
                var fileName = _storageService.SaveImage(_selectedKondisiAwalPath, idBarang, "kondisi_awal");
                item.KondisiAwalPath = fileName;
                Debug.WriteLine($"Saved kondisi awal photo as: {fileName}");
            }

            if (_selectedKondisiAkhirPath != null)
            {
                Debug.WriteLine($"Saving new kondisi akhir photo from: {_selectedKondisiAkhirPath}");
                var fileName = _storageService.SaveImage(_selectedKondisiAkhirPath, idBarang, "kondisi_akhir");
                item.KondisiAkhirPath = fileName;
                Debug.WriteLine($"Saved kondisi akhir photo as: {fileName}");
            }
        }

        return item;
    }

    private void SetInputsFromItem(Item item)
    {
        if (item == null) return;

        Debug.WriteLine($"Setting inputs from item: {item.IdBarang}");

        // Store current item data for reference
        _currentFotoPath = item.FotoPath;
        _currentKondisiAwalPath = item.KondisiAwalPath;
        _currentKondisiAkhirPath = item.KondisiAkhirPath;

        Debug.WriteLine($"Item foto paths: Main={item.FotoPath}, KondisiAwal={item.KondisiAwalPath}, KondisiAkhir={item.KondisiAkhirPath}");
        
        // Don't reset anything based on _currentEditingId
        // Just populate the form with current item data
        txtIdBarang.Text = item.IdBarang;
        txtNamaBarang.Text = item.NamaBarang;
        txtMerekBarang.Text = item.MerekBarang;
        txtHargaBarang.Text = item.HargaBarang.ToString("N0", CultureInfo.InvariantCulture);
        dpTanggalPerolehan.SelectedDate = item.TanggalPerolehan;
        dpTanggalPinjam.SelectedDate = item.TanggalPinjam;
        txtNamaPengguna.Text = item.NamaPengguna;
        txtBidang.Text = item.Bidang;
        txtKeteranganBarang.Text = item.KeteranganBarang;

        // Reset all images first
        imgBarang.Source = null;
        imgKondisiAwal.Source = null;
        imgKondisiAkhir.Source = null;
        noImagePlaceholder.Visibility = Visibility.Visible;
        noImagePlaceholderKondisiAwal.Visibility = Visibility.Visible;
        noImagePlaceholderKondisiAkhir.Visibility = Visibility.Visible;

        try
        {
            // Ensure the application captures image loading issues by using a more explicit method
            LoadItemImages(item);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading photos: {ex.Message}");
        }

        txtIdBarang.IsEnabled = false;
        _isEditMode = true;
    }
    
    private void LoadItemImages(Item item)
    {
        Debug.WriteLine("LoadItemImages - Started loading images for item");
        
        // Load main photo if it exists
        if (!string.IsNullOrEmpty(item.FotoPath))
        {
            Debug.WriteLine($"Loading main photo: {item.FotoPath}");
            
            // Try both the primary image location and the asset location
            string imagePath = Path.Combine(_storageService.ImagePath, item.FotoPath);
            string assetPath = Path.Combine(_storageService.AssetGambarPath, item.FotoPath);
            
            Debug.WriteLine($"Main photo paths to check: \n1. {imagePath} \n2. {assetPath}");
            
            if (File.Exists(imagePath))
            {
                Debug.WriteLine($"Main photo found in primary location: {imagePath}");
                LoadImageIntoControl(imagePath, imgBarang, noImagePlaceholder);
            }
            else if (File.Exists(assetPath))
            {
                Debug.WriteLine($"Main photo found in asset location: {assetPath}");
                LoadImageIntoControl(assetPath, imgBarang, noImagePlaceholder);
            }
            else
            {
                Debug.WriteLine($"Main photo file not found in either location: {item.FotoPath}");
            }
        }

        // Load kondisi awal photo if it exists
        if (!string.IsNullOrEmpty(item.KondisiAwalPath))
        {
            Debug.WriteLine($"Loading kondisi awal photo: {item.KondisiAwalPath}");
            
            string imagePath = Path.Combine(_storageService.ImagePath, item.KondisiAwalPath);
            string assetPath = Path.Combine(_storageService.AssetGambarPath, item.KondisiAwalPath);
            
            Debug.WriteLine($"Kondisi awal photo paths to check: \n1. {imagePath} \n2. {assetPath}");
            
            if (File.Exists(imagePath))
            {
                Debug.WriteLine($"Kondisi awal photo found in primary location: {imagePath}");
                LoadImageIntoControl(imagePath, imgKondisiAwal, noImagePlaceholderKondisiAwal);
            }
            else if (File.Exists(assetPath))
            {
                Debug.WriteLine($"Kondisi awal photo found in asset location: {assetPath}");
                LoadImageIntoControl(assetPath, imgKondisiAwal, noImagePlaceholderKondisiAwal);
            }
            else
            {
                Debug.WriteLine($"Kondisi awal photo file not found in either location: {item.KondisiAwalPath}");
            }
        }

        // Load kondisi akhir photo if it exists
        if (!string.IsNullOrEmpty(item.KondisiAkhirPath))
        {
            Debug.WriteLine($"Loading kondisi akhir photo: {item.KondisiAkhirPath}");
            
            string imagePath = Path.Combine(_storageService.ImagePath, item.KondisiAkhirPath);
            string assetPath = Path.Combine(_storageService.AssetGambarPath, item.KondisiAkhirPath);
            
            Debug.WriteLine($"Kondisi akhir photo paths to check: \n1. {imagePath} \n2. {assetPath}");
            
            if (File.Exists(imagePath))
            {
                Debug.WriteLine($"Kondisi akhir photo found in primary location: {imagePath}");
                LoadImageIntoControl(imagePath, imgKondisiAkhir, noImagePlaceholderKondisiAkhir);
            }
            else if (File.Exists(assetPath))
            {
                Debug.WriteLine($"Kondisi akhir photo found in asset location: {assetPath}");
                LoadImageIntoControl(assetPath, imgKondisiAkhir, noImagePlaceholderKondisiAkhir);
            }
            else
            {
                Debug.WriteLine($"Kondisi akhir photo file not found in either location: {item.KondisiAkhirPath}");
            }
        }
    }
    
    private void LoadImageIntoControl(string imagePath, System.Windows.Controls.Image imageControl, UIElement placeholder)
    {
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
            bitmap.EndInit();
            
            imageControl.Source = bitmap;
            if (placeholder != null)
            {
                placeholder.Visibility = Visibility.Collapsed;
            }
            Debug.WriteLine($"Successfully loaded image from {imagePath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading image from {imagePath}: {ex.Message}");
            if (placeholder != null)
            {
                placeholder.Visibility = Visibility.Visible;
            }
        }
    }

    private async void BtnSimpan_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Get item from inputs
            var item = GetItemFromInputs();
            if (item == null)
            {
                return;
            }

            // Update existing or add new item
            if (_isEditMode && _currentEditingId != null)
            {
                // Update existing item
                var existingItemIndex = _daftarBarang.FindIndex(i => i.IdBarang == _currentEditingId);
                if (existingItemIndex >= 0)
                {
                    // Make sure to keep the original ID for proper referencing in the list
                    item.IdBarang = _currentEditingId;
                    _daftarBarang[existingItemIndex] = item;
                    MsgBox.Show("Data barang berhasil diupdate.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                // Add new item
                _daftarBarang.Add(item);
                MsgBox.Show("Data barang berhasil disimpan.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            // Save data to file
            await _storageService.SaveItemsAsync(_daftarBarang);

            // Refresh DataGrid
            dgBarang.ItemsSource = null;
            dgBarang.ItemsSource = _daftarBarang;

            // Reset fields and selected image paths
            ClearInputs();
            _selectedImagePath = null;
            _selectedKondisiAwalPath = null;
            _selectedKondisiAkhirPath = null;
            _currentFotoPath = null;
            _currentKondisiAwalPath = null;
            _currentKondisiAkhirPath = null;
            _isEditMode = false;
            _currentEditingId = null;
            _selectedItem = null;

            // Reset image placeholders
            imgBarang.Source = null;
            imgKondisiAwal.Source = null;
            imgKondisiAkhir.Source = null;
            noImagePlaceholder.Visibility = Visibility.Visible;
            noImagePlaceholderKondisiAwal.Visibility = Visibility.Visible;
            noImagePlaceholderKondisiAkhir.Visibility = Visibility.Visible;
            
            // Switch to list tab
            tabControl.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MsgBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private bool HasUnsavedChanges()
    {
        if (_selectedItem == null) return false;
        
        return txtNamaBarang.Text != _selectedItem.NamaBarang ||
               txtMerekBarang.Text != _selectedItem.MerekBarang ||
               txtHargaBarang.Text.Replace("Rp", "").Replace(".", "").Replace(",", "").Trim() != _selectedItem.HargaBarang.ToString(CultureInfo.InvariantCulture) ||
               dpTanggalPerolehan.SelectedDate != _selectedItem.TanggalPerolehan ||
               dpTanggalPinjam.SelectedDate != _selectedItem.TanggalPinjam ||
               txtNamaPengguna.Text != _selectedItem.NamaPengguna ||
               txtBidang.Text != _selectedItem.Bidang ||
               txtKeteranganBarang.Text != _selectedItem.KeteranganBarang ||
               !string.IsNullOrEmpty(_selectedImagePath) ||
               !string.IsNullOrEmpty(_selectedKondisiAwalPath) ||
               !string.IsNullOrEmpty(_selectedKondisiAkhirPath);
    }

    private void BtnBaru_Click(object sender, RoutedEventArgs e)
    {
        if (HasUnsavedChanges())
        {
            var result = MsgBox.Show("Apakah Anda yakin ingin membuat data baru? Data yang belum disimpan akan hilang.", 
                "Konfirmasi", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.No)
            {
                return;
            }
        }
        
        ClearInputs();
        
        // Pastikan tombol foto selalu aktif untuk data baru
        btnPilihFoto.IsEnabled = true;
        btnPilihFotoKondisiAwal.IsEnabled = true;
        btnPilihFotoKondisiAkhir.IsEnabled = true;
    }

    private async void BtnHapus_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedItem == null)
        {
            MsgBox.Show("Silakan pilih barang yang akan dihapus!", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var result = MsgBox.Show($"Anda yakin ingin menghapus barang dengan ID {_selectedItem.IdBarang}?", 
                                    "Konfirmasi", 
                                    MessageBoxButton.YesNo, 
                                    MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                bool success = await _storageService.DeleteItemAsync(_selectedItem.IdBarang);
                if (success)
                {
                    MsgBox.Show("Data barang berhasil dihapus!", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
                    ClearInputs();
                    await LoadData();
                }
                else
                {
                    MsgBox.Show("Gagal menghapus data barang!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MsgBox.Show($"Terjadi kesalahan: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void DgBarang_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // When the selection changes, we need to properly clear everything first
        
        // Clear the selected paths to prevent old paths from being used
        _selectedImagePath = null;
        _selectedKondisiAwalPath = null;
        _selectedKondisiAkhirPath = null;
        
        // Clear image previews
        imgBarang.Source = null;
        imgKondisiAwal.Source = null;
        imgKondisiAkhir.Source = null;
        
        // Enable photo selection buttons
        btnPilihFoto.IsEnabled = true;
        btnPilihFotoKondisiAwal.IsEnabled = true;
        btnPilihFotoKondisiAkhir.IsEnabled = true;

        // Get the newly selected item
        Item selectedItem = dgBarang.SelectedItem as Item;
        if (selectedItem != null)
        {
            try
            {
                // Clear the previous selected item and store the new one properly
                _selectedItem = selectedItem;
                
                // Store the ID for editing
                _currentEditingId = selectedItem.IdBarang;
                
                // Use SetInputsFromItem to populate the UI
                SetInputsFromItem(selectedItem);
                
                // Set edit mode flag
                _isEditMode = true;
                
                // Enable buttons that require a selection
                btnSimpan.IsEnabled = true;
                btnHapus.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MsgBox.Show($"Error loading item: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        else
        {
            // No selection, reset everything
            _isEditMode = false;
            _currentEditingId = null;
            _selectedItem = null;
            
            // Disable buttons that require a selection
            btnHapus.IsEnabled = false;
        }
    }

    private void BtnCari_Click(object sender, RoutedEventArgs e)
    {
        PerformSearch();
    }

    private void TxtCari_TextChanged(object sender, TextChangedEventArgs e)
    {
        PerformSearch();
    }

    private void PerformSearch()
    {
        string searchTerm = txtCari.Text.ToLower();

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            dgBarang.ItemsSource = _daftarBarang;
            return;
        }

        var filteredItems = _daftarBarang.Where(item =>
            item.IdBarang.ToLower().Contains(searchTerm) ||
            item.NamaBarang.ToLower().Contains(searchTerm) ||
            item.MerekBarang.ToLower().Contains(searchTerm) ||
            item.NamaPengguna.ToLower().Contains(searchTerm) ||
            (item.KeteranganBarang != null && item.KeteranganBarang.ToLower().Contains(searchTerm))
        ).ToList();

        dgBarang.ItemsSource = filteredItems;
    }

    private void TxtHargaBarang_LostFocus(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtHargaBarang.Text))
            return;

        string hargaText = txtHargaBarang.Text.Replace("Rp", "").Replace(".", "").Replace(",", "").Trim();
        if (decimal.TryParse(hargaText, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal harga))
        {
            txtHargaBarang.Text = string.Format(CultureInfo.CreateSpecificCulture("id-ID"), "Rp {0:N0}", harga);
        }
        else
        {
            txtHargaBarang.Text = string.Empty;
            MsgBox.Show("Format harga tidak valid.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void TxtHargaBarang_GotFocus(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtHargaBarang.Text))
            return;

        string hargaText = txtHargaBarang.Text.Replace("Rp", "").Replace(".", "").Replace(",", "").Trim();
        if (decimal.TryParse(hargaText, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal harga))
        {
            txtHargaBarang.Text = harga.ToString(CultureInfo.InvariantCulture);
        }
    }

    private void DatePicker_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = true;
    }

    private void BtnBackup_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            int backupNumber = 1;
            var existingBackupFolders = Directory.GetDirectories(_backupFolderPath, "backup_*_*");
            if (existingBackupFolders.Length > 0)
            {
                var lastBackupNumber = existingBackupFolders
                    .Select(f => Path.GetFileName(f).Split('_')[1])
                    .Where(n => int.TryParse(n, out _))
                    .Select(int.Parse)
                    .OrderByDescending(n => n)
                    .FirstOrDefault();
                backupNumber = lastBackupNumber + 1;
            }

            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            
            // Buat folder khusus untuk backup ini
            string backupFolderName = $"backup_{backupNumber}_{timestamp}";
            string backupFolderPath = Path.Combine(_backupFolderPath, backupFolderName);
            Directory.CreateDirectory(backupFolderPath);
            
            // Backup file JSON
            string backupFileName = "data.json";
            string backupFilePath = Path.Combine(backupFolderPath, backupFileName);
            string sourceFilePath = _storageService.FilePath;

            if (File.Exists(sourceFilePath))
            {
                // Copy file JSON
                File.Copy(sourceFilePath, backupFilePath);
                
                // Buat folder untuk gambar
                string backupImagesPath = Path.Combine(backupFolderPath, "Images");
                Directory.CreateDirectory(backupImagesPath);
                
                // Copy semua gambar dari folder Images
                string sourceImagesPath = _storageService.ImagePath;
                if (Directory.Exists(sourceImagesPath))
                {
                    foreach (string imageFile in Directory.GetFiles(sourceImagesPath))
                    {
                        string fileName = Path.GetFileName(imageFile);
                        string destImagePath = Path.Combine(backupImagesPath, fileName);
                        File.Copy(imageFile, destImagePath);
                    }
                }
                
                MsgBox.Show($"Data dan gambar berhasil di-backup ke:\n{backupFolderPath}", 
                               "Backup Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MsgBox.Show("File data utama (items.json) tidak ditemukan. Backup dibatalkan.", 
                               "Backup Gagal", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            MsgBox.Show($"Terjadi kesalahan saat backup: {ex.Message}", 
                           "Backup Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnLoad_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var folderBrowserDialog = new WinForms.FolderBrowserDialog
            {
                Description = "Pilih folder backup yang akan di-restore",
                UseDescriptionForTitle = true,
                SelectedPath = _backupFolderPath
            };

            if (folderBrowserDialog.ShowDialog() == WinForms.DialogResult.OK)
            {
                string selectedBackupFolderPath = folderBrowserDialog.SelectedPath;
                string backupDataFile = Path.Combine(selectedBackupFolderPath, "data.json");
                string backupImagesPath = Path.Combine(selectedBackupFolderPath, "Images");

                if (!File.Exists(backupDataFile))
                {
                    MsgBox.Show("File data.json tidak ditemukan di folder backup!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!Directory.Exists(backupImagesPath))
                {
                    var result = MsgBox.Show("Folder Images tidak ditemukan di backup. Lanjutkan tanpa memulihkan gambar?", "Peringatan", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.No)
                    {
                        return;
                    }
                }

                var confirmLoadResult = MsgBox.Show("Memuat backup akan menimpa data dan gambar saat ini.\nAnda yakin ingin melanjutkan?",
                                                    "Konfirmasi Load Backup",
                                                    MessageBoxButton.YesNo,
                                                    MessageBoxImage.Warning);

                if (confirmLoadResult == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Load data JSON
                        string json = await File.ReadAllTextAsync(backupDataFile);
                        var backupItems = JsonSerializer.Deserialize<List<Item>>(json);

                        if (backupItems == null)
                        {
                            MsgBox.Show("File backup tidak valid atau kosong.", "Load Gagal", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        // Clear current images from UI
                        imgBarang.Source = null;
                        imgKondisiAwal.Source = null;
                        imgKondisiAkhir.Source = null;
                        
                        // Force garbage collection to release file handles
                        GC.Collect();
                        GC.WaitForPendingFinalizers();

                        // Ensure AssetGambar directory exists
                        Directory.CreateDirectory(_storageService.ImagePath);
                        
                        // Salin data JSON
                        await _storageService.SaveItemsAsync(backupItems);
                        
                        // Salin gambar jika folder gambar backup ada
                        if (Directory.Exists(backupImagesPath))
                        {
                            try
                            {
                                // Hapus semua file gambar lama
                                if (Directory.Exists(_storageService.ImagePath))
                                {
                                    foreach (string imageFile in Directory.GetFiles(_storageService.ImagePath))
                                    {
                                        try
                                        {
                                            File.Delete(imageFile);
                                        }
                                        catch (IOException)
                                        {
                                            // If file is locked, try to force delete
                                            GC.Collect();
                                            GC.WaitForPendingFinalizers();
                                            File.Delete(imageFile);
                                        }
                                    }
                                }
                                
                                // Salin semua gambar dari backup
                                foreach (string imageFile in Directory.GetFiles(backupImagesPath))
                                {
                                    string fileName = Path.GetFileName(imageFile);
                                    string destImagePath = Path.Combine(_storageService.ImagePath, fileName);
                                    File.Copy(imageFile, destImagePath, true);
                                }
                            }
                            catch (Exception ex)
                            {
                                MsgBox.Show($"Terjadi kesalahan saat memproses file gambar: {ex.Message}\nProses restore data tetap dilanjutkan.", 
                                    "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                        }

                        await LoadData();
                        ClearInputs();

                        MsgBox.Show("Data dan gambar berhasil dimuat dari backup.", 
                                  "Load Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MsgBox.Show($"Terjadi kesalahan saat load backup: {ex.Message}", 
                                  "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MsgBox.Show($"Terjadi kesalahan: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task<FileInfo?> PilihFoto(object sender)
    {
        try
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Pilih Foto",
                Filter = "Image files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|All files (*.*)|*.*",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var file = new FileInfo(openFileDialog.FileName);
                if (file.Exists)
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(file.FullName);
                    bitmap.EndInit();
                    
                    // Clear previous selection first to avoid carrying over old selections
                    if (sender == btnPilihFoto)
                    {
                        _selectedImagePath = file.FullName;
                        Debug.WriteLine($"Selected main photo: {_selectedImagePath}");
                        
                        // Update UI
                        imgBarang.Source = bitmap;
                        noImagePlaceholder.Visibility = Visibility.Collapsed;
                        Debug.WriteLine("Main photo preview updated");
                    }
                    else if (sender == btnPilihFotoKondisiAwal)
                    {
                        _selectedKondisiAwalPath = file.FullName;
                        Debug.WriteLine($"Selected kondisi awal photo: {_selectedKondisiAwalPath}");
                        
                        // Update UI
                        imgKondisiAwal.Source = bitmap;
                        noImagePlaceholderKondisiAwal.Visibility = Visibility.Collapsed;
                        Debug.WriteLine("Kondisi awal preview updated");
                    }
                    else if (sender == btnPilihFotoKondisiAkhir)
                    {
                        _selectedKondisiAkhirPath = file.FullName;
                        Debug.WriteLine($"Selected kondisi akhir photo: {_selectedKondisiAkhirPath}");
                        
                        // Update UI
                        imgKondisiAkhir.Source = bitmap;
                        noImagePlaceholderKondisiAkhir.Visibility = Visibility.Collapsed;
                        Debug.WriteLine("Kondisi akhir preview updated");
                    }
                    
                    return file;
                }
                else
                {
                    Debug.WriteLine($"Selected file does not exist: {file.FullName}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error selecting image: {ex.Message}");
            MsgBox.Show($"Error selecting image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        
        return null;
    }

    private void UpdateActiveButton(int tabIndex)
    {
        // Reset all buttons to default style
        ResetSidebarButtons();
        
        // Mark the active button
        switch (tabIndex)
        {
            case 0: // Daftar Barang
                MarkButtonActive(btnListBarang);
                break;
            case 1: // Form (Tambah/Edit Barang)
                if (_isEditMode)
                    MarkButtonActive(btnEditBarang);
                else
                    MarkButtonActive(btnTambahBarang);
                break;
        }
    }
    
    private void ResetSidebarButtons()
    {
        // Reset all sidebar buttons to default state
        btnListBarang.Background = System.Windows.Media.Brushes.Transparent;
        btnTambahBarang.Background = System.Windows.Media.Brushes.Transparent;
        btnEditBarang.Background = System.Windows.Media.Brushes.Transparent;
    }
    
    private void MarkButtonActive(System.Windows.Controls.Button button)
    {
        button.Background = new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1565C0"));
    }

    // Tambahkan method untuk sidebar
    private void BtnListBarang_Click(object sender, RoutedEventArgs e)
    {
        // Tampilkan tab daftar barang
        tabControl.SelectedIndex = 0;
        UpdateActiveButton(0);
    }

    private void BtnTambahBarang_Click(object sender, RoutedEventArgs e)
    {
        // Reset form dan tampilkan tab tambah barang
        ClearInputs();
        
        // Set ID otomatis ke nomor terbaru
        int nextId = GetNextId();
        txtIdBarang.Text = nextId.ToString();
        
        // Enable ID field untuk pengguna jika ingin mengubahnya
        txtIdBarang.IsEnabled = true;
        
        // Buka tab form
        tabControl.SelectedIndex = 1;
        UpdateActiveButton(1);
    }

    private void BtnEditBarang_Click(object sender, RoutedEventArgs e)
    {
        // Cek apakah ada item yang dipilih
        if (dgBarang.SelectedItem is Item selectedItem)
        {
            // Set form berdasarkan item yang dipilih
            SetInputsFromItem(selectedItem);
            tabControl.SelectedIndex = 1;
            UpdateActiveButton(1);
        }
        else
        {
            MsgBox.Show("Pilih item dari daftar terlebih dahulu untuk diedit.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            tabControl.SelectedIndex = 0;
            UpdateActiveButton(0);
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Set tab default ke daftar barang
        tabControl.SelectedIndex = 0;
        UpdateActiveButton(0);
    }

    private async void BtnHapusItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button && button.Tag is string itemId)
        {
            var targetItem = _daftarBarang.FirstOrDefault(item => item.IdBarang == itemId);
            
            if (targetItem != null)
            {
                var result = MsgBox.Show($"Anda yakin ingin menghapus barang dengan ID {itemId}?", 
                                         "Konfirmasi", 
                                         MessageBoxButton.YesNo, 
                                         MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        bool success = await _storageService.DeleteItemAsync(itemId);
                        if (success)
                        {
                            MsgBox.Show("Data barang berhasil dihapus!", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
                            
                            // If the current editing item is the one being deleted, clear the inputs
                            if (_currentEditingId == itemId)
                            {
                                ClearInputs();
                            }
                            
                            await LoadData();
                        }
                        else
                        {
                            MsgBox.Show("Gagal menghapus data barang!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MsgBox.Show($"Terjadi kesalahan: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }

    private void DgBarang_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // Periksa apakah ada item yang dipilih di DataGrid
        if (dgBarang.SelectedItem is Item selectedItem)
        {
            // Set form berdasarkan item yang dipilih
            SetInputsFromItem(selectedItem);
            
            // Beralih ke tab form untuk edit barang
            tabControl.SelectedIndex = 1;
        }
    }

    private async void BtnPilihFoto_Click(object sender, RoutedEventArgs e)
    {
        await PilihFoto(sender);
    }

    private async void BtnPilihFotoKondisiAwal_Click(object sender, RoutedEventArgs e)
    {
        await PilihFoto(sender);
    }

    private async void BtnPilihFotoKondisiAkhir_Click(object sender, RoutedEventArgs e)
    {
        await PilihFoto(sender);
    }

    private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (tabControl.SelectedIndex >= 0)
        {
            UpdateActiveButton(tabControl.SelectedIndex);
        }
    }

    private void BtnPrintBarang_Click(object sender, RoutedEventArgs e)
    {
        PrintBarangList();
    }

    private void PrintBarangList()
    {
        try
        {
            // Dialog untuk mengumpulkan informasi cetak
            var printSettingsDialog = new System.Windows.Window
            {
                Title = "Pengaturan Dokumen",
                Width = 400,
                Height = 380,
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen,
                ResizeMode = System.Windows.ResizeMode.NoResize,
                WindowStyle = System.Windows.WindowStyle.ToolWindow
            };

            // Grid utama
            var mainGrid = new System.Windows.Controls.Grid();
            mainGrid.Margin = new System.Windows.Thickness(20);
            mainGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
            mainGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });

            // Header
            var headerText = new System.Windows.Controls.TextBlock
            {
                Text = "Atur Informasi Dokumen",
                FontSize = 16,
                FontWeight = System.Windows.FontWeights.Bold,
                Margin = new System.Windows.Thickness(0, 0, 0, 15)
            };
            System.Windows.Controls.Grid.SetRow(headerText, 0);
            mainGrid.Children.Add(headerText);

            // Form panel
            var formPanel = new System.Windows.Controls.StackPanel();
            System.Windows.Controls.Grid.SetRow(formPanel, 1);
            mainGrid.Children.Add(formPanel);

            // Kepala Dinas
            var kadinLabel = new System.Windows.Controls.TextBlock
            {
                Text = "Nama Kepala Dinas:",
                Margin = new System.Windows.Thickness(0, 0, 0, 5)
            };
            formPanel.Children.Add(kadinLabel);

            var kadinTextBox = new System.Windows.Controls.TextBox
            {
                Text = "Windy Prihastari, S.STP., M.Si.",
                Margin = new System.Windows.Thickness(0, 0, 0, 10),
                Padding = new System.Windows.Thickness(5)
            };
            formPanel.Children.Add(kadinTextBox);

            // NIP
            var nipLabel = new System.Windows.Controls.TextBlock
            {
                Text = "NIP Kepala Dinas:",
                Margin = new System.Windows.Thickness(0, 0, 0, 5)
            };
            formPanel.Children.Add(nipLabel);

            var nipTextBox = new System.Windows.Controls.TextBox
            {
                Text = "19781028 199802 2 001",
                Margin = new System.Windows.Thickness(0, 0, 0, 10),
                Padding = new System.Windows.Thickness(5)
            };
            formPanel.Children.Add(nipTextBox);

            // Tanggal
            var tanggalLabel = new System.Windows.Controls.TextBlock
            {
                Text = "Tanggal Dokumen:",
                Margin = new System.Windows.Thickness(0, 0, 0, 5)
            };
            formPanel.Children.Add(tanggalLabel);

            var tanggalPicker = new System.Windows.Controls.DatePicker
            {
                SelectedDate = DateTime.Now,
                Margin = new System.Windows.Thickness(0, 0, 0, 20)
            };
            formPanel.Children.Add(tanggalPicker);

            // Buttons panel
            var buttonsPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                Margin = new System.Windows.Thickness(0, 15, 0, 0)
            };
            System.Windows.Controls.Grid.SetRow(buttonsPanel, 2);
            mainGrid.Children.Add(buttonsPanel);

            var cancelButton = new System.Windows.Controls.Button
            {
                Content = "Batal",
                Width = 100,
                Height = 30,
                Margin = new System.Windows.Thickness(0, 0, 10, 0)
            };
            cancelButton.Click += (s, e) => printSettingsDialog.Close();
            buttonsPanel.Children.Add(cancelButton);

            var saveButton = new System.Windows.Controls.Button
            {
                Content = "Simpan Excel",
                Width = 120,
                Height = 30,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.DarkBlue),
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White)
            };
            buttonsPanel.Children.Add(saveButton);

            printSettingsDialog.Content = mainGrid;

            // Save button only calls Excel export
            saveButton.Click += (s, e) =>
            {
                printSettingsDialog.Close();
                ExportToExcel(kadinTextBox.Text, nipTextBox.Text, tanggalPicker.SelectedDate ?? DateTime.Now);
            };

            printSettingsDialog.ShowDialog();
        }
        catch (Exception ex)
        {
            MsgBox.Show($"Terjadi kesalahan: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExportToExcel(string namaPejabat, string nipPejabat, DateTime tanggalDokumen)
    {
        try
        {
            // Create a SaveFileDialog
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                DefaultExt = "xlsx",
                FileName = $"Daftar_Barang_{DateTime.Now:yyyyMMdd}.xlsx", // Ensure .xlsx extension
                Title = "Simpan Dokumen Excel",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                string outputPath = saveFileDialog.FileName;
                
                // Ensure directory exists
                string directoryPath = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                {
                    try { Directory.CreateDirectory(directoryPath); }
                    catch (Exception ex)
                    {
                        MsgBox.Show($"Gagal membuat direktori: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                
                // Set EPPlus license context
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                try
                {
                    // Create package in memory first
                    using (var package = new ExcelPackage())
                    {
                        var worksheet = package.Workbook.Worksheets.Add("Daftar Barang");
                        
                        // Set default font
                        worksheet.Cells.Style.Font.Name = "Times New Roman";
                        worksheet.Cells.Style.Font.Size = 11;
                        
                        int totalColumns = 8; // Define total columns for easy reference
                        int row = 1;

                        // Header information
                        worksheet.Cells[row, 1, row, totalColumns].Merge = true;
                        worksheet.Cells[row, 1].Value = "PEMERINTAH PROVINSI KALIMANTAN BARAT";
                        worksheet.Cells[row, 1].Style.Font.Bold = true;
                        worksheet.Cells[row, 1].Style.Font.Size = 14;
                        worksheet.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        row++;

                        worksheet.Cells[row, 1, row, totalColumns].Merge = true;
                        worksheet.Cells[row, 1].Value = "DINAS PEMUDA, OLAHRAGA DAN PARIWISATA";
                        worksheet.Cells[row, 1].Style.Font.Bold = true;
                        worksheet.Cells[row, 1].Style.Font.Size = 16;
                        worksheet.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        row++;
                        
                        worksheet.Cells[row, 1, row, totalColumns].Merge = true;
                        worksheet.Cells[row, 1].Value = "Jl. Ki Mangunsarkoro No. 1 Telp. (0355) 322220 Tulungagung";
                        worksheet.Cells[row, 1].Style.Font.Size = 12;
                        worksheet.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        row++;

                        worksheet.Cells[row, 1, row, totalColumns].Merge = true;
                        worksheet.Cells[row, 1].Value = "Telp.  (0561) 742838 Faximile (0561) 739644";
                        worksheet.Cells[row, 1].Style.Font.Size = 12;
                        worksheet.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        row++;
                        
                        worksheet.Cells[row, 1, row, totalColumns].Merge = true;
                        worksheet.Cells[row, 1].Value = "Laman www.disporapar@kalbarprov.go.id Pos-el disporapar.kalbarprov.go.id";
                        worksheet.Cells[row, 1].Style.Font.Size = 11;
                        worksheet.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        row++;
                        
                        // Add a border line
                        worksheet.Cells[row, 1, row, totalColumns].Style.Border.Bottom.Style = ExcelBorderStyle.Thick;
                        row++;
                        row++; // Extra space
                        
                        worksheet.Cells[row, 1, row, totalColumns].Merge = true;
                        worksheet.Cells[row, 1].Value = "DAFTAR INVENTARIS BARANG";
                        worksheet.Cells[row, 1].Style.Font.Bold = true;
                        worksheet.Cells[row, 1].Style.Font.Size = 16;
                        worksheet.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        row++;
                        
                        worksheet.Cells[row, 1, row, totalColumns].Merge = true;
                        worksheet.Cells[row, 1].Value = $"TAHUN {DateTime.Now.Year}";
                        worksheet.Cells[row, 1].Style.Font.Bold = true;
                        worksheet.Cells[row, 1].Style.Font.Size = 14;
                        worksheet.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        row++;
                        row++; // Extra space
                        
                        // --- Start Data Table --- 
                        int dataTableHeaderRow = row;

                        // Table headers
                        string[] headers = { "ID", "Nama Barang", "Merek", "Harga", "Tgl Perolehan", "Tgl Pinjam", "Pengguna", "Bidang" };
                        for (int col = 1; col <= headers.Length; col++)
                        {
                            worksheet.Cells[row, col].Value = headers[col - 1];
                        }
                        row++;
                        
                        // Data rows
                        int dataStartRow = row;
                        foreach (var item in _daftarBarang)
                        {
                            worksheet.Cells[row, 1].Value = item.IdBarang;
                            worksheet.Cells[row, 2].Value = item.NamaBarang;
                            worksheet.Cells[row, 3].Value = item.MerekBarang;
                            worksheet.Cells[row, 4].Value = item.HargaBarang;
                            worksheet.Cells[row, 4].Style.Numberformat.Format = @"\R\p #,##0"; // Rupiah format
                            worksheet.Cells[row, 5].Value = item.TanggalPerolehan;
                            worksheet.Cells[row, 5].Style.Numberformat.Format = "dd/MM/yyyy"; // Date format
                            worksheet.Cells[row, 6].Value = item.TanggalPinjam;
                            worksheet.Cells[row, 6].Style.Numberformat.Format = "dd/MM/yyyy"; // Date format
                            worksheet.Cells[row, 7].Value = item.NamaPengguna;
                            worksheet.Cells[row, 8].Value = item.Bidang;
                            row++;
                        }
                        int dataEndRow = row - 1;
                        // --- End Data Table --- 
                        
                        row++; // Space before summary
                        
                        // --- Start Summary Section --- 
                        int summaryStartRow = row;
                        
                        // worksheet.Cells[row, 2, row, totalColumns].Merge = true; // Merge across all columns for the title
                        worksheet.Cells[row, 3].Value = "RINGKASAN";
                        worksheet.Cells[row, 2].Style.Font.Bold = true;
                        worksheet.Cells[row, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Align title left
                        row++;
                        
                        // Total Jumlah Barang
                        worksheet.Cells[row, 3].Value = "Total Jumlah Barang:"; // Label in Col B
                        worksheet.Cells[row, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Align label left
                        worksheet.Cells[row, 4].Value = _daftarBarang.Count; // Value in Col C
                        worksheet.Cells[row,4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Align value right
                        row++;
                        
                        // Total Nilai Inventaris
                        worksheet.Cells[row, 3].Value = "Total Nilai Inventaris:"; // Label in Col B
                        worksheet.Cells[row, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Align label left
                        decimal totalValue = _daftarBarang.Sum(item => item.HargaBarang);
                        worksheet.Cells[row, 4].Value = totalValue; // Value in Col C
                        worksheet.Cells[row, 4].Style.Numberformat.Format = @"\R\p #,##0";
                        worksheet.Cells[row, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Align value right
                        row++;
                        // --- End Summary Section --- 

                        row++;
                        row++; // Space before signature

                        // --- Start Signature Section --- 
                        int signatureStartRow = row;
                        int signatureColumn = totalColumns; // Align signature to the last column
                        
                        worksheet.Cells[row, signatureColumn].Value = $"Tulungagung, {tanggalDokumen:dd MMMM yyyy}";
                        row++;
                        
                        worksheet.Cells[row, signatureColumn].Value = "Kepala Dinas";
                        row += 4; // Space for signature
                        
                        worksheet.Cells[row, signatureColumn].Value = namaPejabat;
                        row++;
                        
                        worksheet.Cells[row, signatureColumn].Value = $"NIP. {nipPejabat}";
                        int signatureEndRow = row;
                        // --- End Signature Section --- 

                        // === Apply Styling and Print Settings ===

                        // Adjust column widths precisely
                        worksheet.Column(1).Width = 6;  // ID
                        worksheet.Column(2).Width = 35; // Nama Barang
                        worksheet.Column(3).Width = 25; // Merek
                        worksheet.Column(4).Width = 18; // Harga
                        worksheet.Column(5).Width = 15; // Tgl Perolehan
                        worksheet.Column(6).Width = 15; // Tgl Pinjam
                        worksheet.Column(7).Width = 30; // Pengguna
                        worksheet.Column(8).Width = 20; // Bidang

                        // Style the main title headers (Rows 1-5)
                        using (var range = worksheet.Cells[1, 1, 5, totalColumns])
                        {
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.WrapText = false; 
                        }
                        worksheet.Cells[1, 1, 2, totalColumns].Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(0, 32, 96)); // Dark Blue
                        worksheet.Cells[3, 1, 5, totalColumns].Style.Font.Size = 10; 

                        // Style the table headers (dataTableHeaderRow)
                        using (var range = worksheet.Cells[dataTableHeaderRow, 1, dataTableHeaderRow, totalColumns])
                        {
                            range.Style.Font.Bold = true;
                            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(220, 220, 220)); // Lighter Gray
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                            range.Style.Border.Bottom.Style = ExcelBorderStyle.Thick;
                            range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                            range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        }
                        
                        // Apply alternating row colors and borders to data rows (dataStartRow to dataEndRow)
                        for (int i = dataStartRow; i <= dataEndRow; i++)
                        {
                            // Apply borders first
                            using (var range = worksheet.Cells[i, 1, i, totalColumns])
                            {
                                range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top; 
                                range.Style.WrapText = true; 
                            }
                            
                            // Apply alternating background color
                            if (i % 2 == 0) // Even data rows 
                            {
                                using (var range = worksheet.Cells[i, 1, i, totalColumns])
                                {
                                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(242, 242, 242)); // Very Light Gray
                                }
                            }
                            // Ensure Harga column is right-aligned
                            worksheet.Cells[i, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                             // Center align date columns
                            worksheet.Cells[i, 5, i, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; 
                        }

                        // Style the Summary section 
                        worksheet.Cells[summaryStartRow, 1].Style.Font.Size = 12; // RINGKASAN title
                        worksheet.Cells[summaryStartRow + 1, 3, summaryStartRow + 2, 3].Style.Font.Bold = true; // Values Bold (Column C)

                        // Style Signature Section (Right Align All in the signature column)
                        using(var range = worksheet.Cells[signatureStartRow, signatureColumn, signatureEndRow, signatureColumn])
                        {
                             range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        }
                        worksheet.Cells[signatureEndRow - 1, signatureColumn].Style.Font.Bold = true; // Bold Name

                        // Print Settings
                        worksheet.PrinterSettings.PaperSize = ePaperSize.A4;
                        worksheet.PrinterSettings.Orientation = eOrientation.Landscape;
                        worksheet.PrinterSettings.FitToPage = true; 
                        worksheet.PrinterSettings.FitToWidth = 1;  
                        worksheet.PrinterSettings.FitToHeight = 0; 
                        worksheet.PrinterSettings.PrintArea = worksheet.Cells[1, 1, signatureEndRow, totalColumns]; // Print from A1 to last cell
                        worksheet.PrinterSettings.HorizontalCentered = true;
                        worksheet.PrinterSettings.VerticalCentered = false; 
                        worksheet.PrinterSettings.TopMargin = 0.75M;   
                        worksheet.PrinterSettings.BottomMargin = 0.75M;
                        worksheet.PrinterSettings.LeftMargin = 0.5M;   
                        worksheet.PrinterSettings.RightMargin = 0.5M;
                        worksheet.PrinterSettings.HeaderMargin = 0.3M;
                        worksheet.PrinterSettings.FooterMargin = 0.3M;
                        worksheet.HeaderFooter.OddHeader.CenteredText = "&\"Arial,Bold\"&12DAFTAR INVENTARIS BARANG"; 
                        worksheet.HeaderFooter.OddFooter.RightAlignedText = string.Format("Halaman {0} dari {1}", ExcelHeaderFooter.PageNumber, ExcelHeaderFooter.NumberOfPages);
                        worksheet.HeaderFooter.OddFooter.LeftAlignedText = $"Dicetak: {DateTime.Now:dd-MMM-yyyy HH:mm}";

                        // === End Styling and Print Settings ===
                        
                        // Save the file (overwrite if exists)
                        if (File.Exists(outputPath))
                        {
                           try { File.Delete(outputPath); }
                           catch (IOException ex) 
                           { 
                               Debug.WriteLine($"Could not delete existing Excel file: {ex.Message}"); 
                           }
                        }
                        package.SaveAs(new FileInfo(outputPath));
                    }
                    
                    // Try to open the file
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = outputPath,
                            UseShellExecute = true
                        });
                        
                        MsgBox.Show($"File Excel berhasil disimpan di:\n{outputPath}", 
                                "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MsgBox.Show($"File berhasil disimpan, tetapi tidak dapat dibuka otomatis: {ex.Message}\n" +
                                $"Lokasi file: {outputPath}", 
                                "Perhatian", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    Debug.WriteLine($"Excel Unauthorized Access: {ex.Message}\n{ex.StackTrace}");
                    MsgBox.Show($"Akses ditolak saat mencoba menyimpan file Excel: {ex.Message}\nPastikan Anda memiliki izin tulis ke folder tersebut.", 
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (IOException ex) when ((ex.HResult & 0xFFFF) == 32) // ERROR_SHARING_VIOLATION
                {
                    Debug.WriteLine($"Excel File In Use: {ex.Message}\n{ex.StackTrace}");
                    MsgBox.Show($"File Excel sedang digunakan oleh program lain. Harap tutup file tersebut dan coba lagi.", 
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Excel Exception: {ex.Message}\n{ex.StackTrace}");
                    MsgBox.Show($"Terjadi kesalahan saat membuat file Excel: {ex.Message}", 
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        catch (Exception ex)
        {
            MsgBox.Show($"Terjadi kesalahan: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Debug.WriteLine(ex.ToString());
        }
    }
    
    private string FormatRupiah(decimal value)
    {
        return string.Format(CultureInfo.CreateSpecificCulture("id-ID"), "Rp {0:N0}", value);
    }

    private void InstagramIcon_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // URL to open
            string url = "https://www.instagram.com/dan.szee/";

            // Use Process.Start to open the URL in the default browser
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true // Important for opening URLs
            });
        }
        catch (Exception ex)
        {
            // Handle potential errors (e.g., browser not found)
            MsgBox.Show($"Tidak dapat membuka link: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}