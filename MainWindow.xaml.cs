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

namespace ManajemenBarang;

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

        if (!_isEditMode && _daftarBarang.Any(item => item.IdBarang == txtIdBarang.Text))
        {
            MsgBox.Show($"ID Barang '{txtIdBarang.Text}' sudah digunakan. Harap gunakan ID lain.", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
            txtIdBarang.Focus();
            return false;
        }
        
        if (!int.TryParse(txtIdBarang.Text, out _))
        {
            MsgBox.Show("No ID Barang harus berupa angka!", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
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
        ValidateInputs();

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
            
            // Untuk foto utama
            if (_selectedImagePath != null)
            {
                // SaveImage returns the fileName, not a FileInfo
                var fileName = _storageService.SaveImage(_selectedImagePath, originalId, "foto");
                item.FotoPath = fileName;
            }
            else
            {
                item.FotoPath = _selectedItem.FotoPath;
            }

            // Untuk foto kondisi awal
            if (_selectedKondisiAwalPath != null)
            {
                var fileName = _storageService.SaveImage(_selectedKondisiAwalPath, originalId, "kondisi_awal");
                item.KondisiAwalPath = fileName;
            }
            else
            {
                item.KondisiAwalPath = _selectedItem.KondisiAwalPath;
            }

            // Untuk foto kondisi akhir
            if (_selectedKondisiAkhirPath != null)
            {
                var fileName = _storageService.SaveImage(_selectedKondisiAkhirPath, originalId, "kondisi_akhir");
                item.KondisiAkhirPath = fileName;
            }
            else
            {
                item.KondisiAkhirPath = _selectedItem.KondisiAkhirPath;
            }
        }
        else
        {
            // Mode tambah baru
            if (_selectedImagePath != null)
            {
                var fileName = _storageService.SaveImage(_selectedImagePath, idBarang, "foto");
                item.FotoPath = fileName;
            }

            if (_selectedKondisiAwalPath != null)
            {
                var fileName = _storageService.SaveImage(_selectedKondisiAwalPath, idBarang, "kondisi_awal");
                item.KondisiAwalPath = fileName;
            }

            if (_selectedKondisiAkhirPath != null)
            {
                var fileName = _storageService.SaveImage(_selectedKondisiAkhirPath, idBarang, "kondisi_akhir");
                item.KondisiAkhirPath = fileName;
            }
        }

        return item;
    }

    private void SetInputsFromItem(Item item)
    {
        if (item == null) return;

        // Store current item data for reference
        _currentFotoPath = item.FotoPath;
        _currentKondisiAwalPath = item.KondisiAwalPath;
        _currentKondisiAkhirPath = item.KondisiAkhirPath;
        
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

        // Load main photo if it exists
        if (!string.IsNullOrEmpty(item.FotoPath))
        {
            string fullPath = Path.Combine(_storageService.ImagePath, item.FotoPath);
            if (File.Exists(fullPath))
            {
                imgBarang.Source = new BitmapImage(new Uri(fullPath));
                noImagePlaceholder.Visibility = Visibility.Collapsed;
            }
        }

        // Load kondisi awal photo if it exists
        if (!string.IsNullOrEmpty(item.KondisiAwalPath))
        {
            string fullPath = Path.Combine(_storageService.ImagePath, item.KondisiAwalPath);
            if (File.Exists(fullPath))
            {
                imgKondisiAwal.Source = new BitmapImage(new Uri(fullPath));
                noImagePlaceholderKondisiAwal.Visibility = Visibility.Collapsed;
            }
        }

        // Load kondisi akhir photo if it exists
        if (!string.IsNullOrEmpty(item.KondisiAkhirPath))
        {
            string fullPath = Path.Combine(_storageService.ImagePath, item.KondisiAkhirPath);
            if (File.Exists(fullPath))
            {
                imgKondisiAkhir.Source = new BitmapImage(new Uri(fullPath));
                noImagePlaceholderKondisiAkhir.Visibility = Visibility.Collapsed;
            }
        }

        txtIdBarang.IsEnabled = false;
        _isEditMode = true;
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
                    // Clear previous selection first to avoid carrying over old selections
                    if (sender == btnPilihFoto)
                    {
                        _selectedImagePath = file.FullName;
                        
                        // Update UI
                        var image = new BitmapImage(new Uri(file.FullName));
                        imgBarang.Source = image;
                        noImagePlaceholder.Visibility = Visibility.Collapsed;
                    }
                    else if (sender == btnPilihFotoKondisiAwal)
                    {
                        _selectedKondisiAwalPath = file.FullName;
                        
                        // Update UI
                        var image = new BitmapImage(new Uri(file.FullName));
                        imgKondisiAwal.Source = image; 
                        noImagePlaceholderKondisiAwal.Visibility = Visibility.Collapsed;
                    }
                    else if (sender == btnPilihFotoKondisiAkhir)
                    {
                        _selectedKondisiAkhirPath = file.FullName;
                        
                        // Update UI
                        var image = new BitmapImage(new Uri(file.FullName));
                        imgKondisiAkhir.Source = image;
                        noImagePlaceholderKondisiAkhir.Visibility = Visibility.Collapsed;
                    }
                    
                    return file;
                }
            }
        }
        catch (Exception ex)
        {
            MsgBox.Show($"Error selecting image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        
        return null;
    }

    // Tambahkan method untuk sidebar
    private void BtnListBarang_Click(object sender, RoutedEventArgs e)
    {
        // Tampilkan tab daftar barang
        tabControl.SelectedIndex = 0;
    }

    private void BtnTambahBarang_Click(object sender, RoutedEventArgs e)
    {
        // Reset form dan tampilkan tab tambah barang
        ClearInputs();
        tabControl.SelectedIndex = 1;
    }

    private void BtnEditBarang_Click(object sender, RoutedEventArgs e)
    {
        // Cek apakah ada item yang dipilih
        if (dgBarang.SelectedItem is Item selectedItem)
        {
            // Set form berdasarkan item yang dipilih
            SetInputsFromItem(selectedItem);
            tabControl.SelectedIndex = 1;
        }
        else
        {
            MsgBox.Show("Pilih item dari daftar terlebih dahulu untuk diedit.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            tabControl.SelectedIndex = 0;
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Set tab default ke daftar barang
        tabControl.SelectedIndex = 0;
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
}