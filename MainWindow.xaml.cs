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

    public MainWindow()
    {
        InitializeComponent();
        _storageService = new StorageService();

        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _backupFolderPath = Path.Combine(appDataPath, "ManajemenBarang", "Backups");
        Directory.CreateDirectory(_backupFolderPath);

        LoadData();
        ClearInputs();
        
        // Menonaktifkan tombol pilih foto saat pertama kali
        btnPilihFoto.IsEnabled = false;
        btnPilihFotoKondisiAwal.IsEnabled = false;
        btnPilihFotoKondisiAkhir.IsEnabled = false;
        
        dpTanggalPerolehan.SelectedDate = DateTime.Now;
        dpTanggalPinjam.SelectedDate = DateTime.Now;
    }

    private async void LoadData()
    {
        try
        {
            _daftarBarang = await _storageService.GetItemsAsync();
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
        txtIdBarang.Text = GetNextId().ToString();
        txtNamaBarang.Text = string.Empty;
        txtMerekBarang.Text = string.Empty;
        txtHargaBarang.Text = string.Empty;
        dpTanggalPerolehan.SelectedDate = DateTime.Now;
        dpTanggalPinjam.SelectedDate = DateTime.Now;
        txtNamaPengguna.Text = string.Empty;
        txtKeteranganBarang.Text = string.Empty;
        txtKeteranganKondisiAwal.Text = string.Empty;
        txtKeteranganKondisiAkhir.Text = string.Empty;
        
        imgBarang.Source = null;
        imgKondisiAwal.Source = null;
        imgKondisiAkhir.Source = null;
        
        _currentFotoPath = null;
        _currentKondisiAwalPath = null;
        _currentKondisiAkhirPath = null;
        
        _selectedItem = null;
        _isEditMode = false;
        txtIdBarang.IsEnabled = true;
        btnHapus.IsEnabled = false;
        
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
        else if (!decimal.TryParse(txtHargaBarang.Text, NumberStyles.Currency, CultureInfo.CurrentCulture, out _) &&
                 !decimal.TryParse(txtHargaBarang.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
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
        decimal hargaBarang = 0;
        if (decimal.TryParse(txtHargaBarang.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out decimal parsedHarga))
        {
            hargaBarang = parsedHarga;
        } else if (decimal.TryParse(txtHargaBarang.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsedHargaInvariant))
        {
            hargaBarang = parsedHargaInvariant;
        }
        
        var item = new Item
        {
            IdBarang = txtIdBarang.Text,
            NamaBarang = txtNamaBarang.Text,
            MerekBarang = txtMerekBarang.Text,
            HargaBarang = hargaBarang,
            TanggalPerolehan = dpTanggalPerolehan.SelectedDate ?? DateTime.Now,
            TanggalPinjam = dpTanggalPinjam.SelectedDate ?? DateTime.Now,
            NamaPengguna = txtNamaPengguna.Text,
            KeteranganBarang = txtKeteranganBarang.Text,
            KeteranganKondisiAwal = txtKeteranganKondisiAwal.Text,
            KeteranganKondisiAkhir = txtKeteranganKondisiAkhir.Text
        };

        // Save images if selected
        if (_currentFotoPath != null)
        {
            item.FotoPath = _storageService.SaveImage(_currentFotoPath, item.IdBarang, "foto");
        }
        
        if (_currentKondisiAwalPath != null)
        {
            item.KondisiAwalPath = _storageService.SaveImage(_currentKondisiAwalPath, item.IdBarang, "kondisi_awal");
        }
        
        if (_currentKondisiAkhirPath != null)
        {
            item.KondisiAkhirPath = _storageService.SaveImage(_currentKondisiAkhirPath, item.IdBarang, "kondisi_akhir");
        }

        return item;
    }

    private void SetInputsFromItem(Item item)
    {
        if (item == null) return;

        txtIdBarang.Text = item.IdBarang;
        txtNamaBarang.Text = item.NamaBarang;
        txtMerekBarang.Text = item.MerekBarang;
        txtHargaBarang.Text = item.HargaBarang.ToString("N0", CultureInfo.InvariantCulture);
        dpTanggalPerolehan.SelectedDate = item.TanggalPerolehan;
        dpTanggalPinjam.SelectedDate = item.TanggalPinjam;
        txtNamaPengguna.Text = item.NamaPengguna;
        txtKeteranganBarang.Text = item.KeteranganBarang;
        txtKeteranganKondisiAwal.Text = item.KeteranganKondisiAwal;
        txtKeteranganKondisiAkhir.Text = item.KeteranganKondisiAkhir;
        
        // Reset all images and paths first
        imgBarang.Source = null;
        imgKondisiAwal.Source = null;
        imgKondisiAkhir.Source = null;
        _currentFotoPath = null;
        _currentKondisiAwalPath = null;
        _currentKondisiAkhirPath = null;
        
        // Load images if they exist
        if (!string.IsNullOrEmpty(item.FotoPath))
        {
            string fullPath = Path.Combine(_storageService.ImagePath, item.FotoPath);
            if (File.Exists(fullPath))
            {
                imgBarang.Source = new BitmapImage(new Uri(fullPath));
                _currentFotoPath = fullPath;
            }
        }
        
        if (!string.IsNullOrEmpty(item.KondisiAwalPath))
        {
            string fullPath = Path.Combine(_storageService.ImagePath, item.KondisiAwalPath);
            if (File.Exists(fullPath))
            {
                imgKondisiAwal.Source = new BitmapImage(new Uri(fullPath));
                _currentKondisiAwalPath = fullPath;
            }
        }
        
        if (!string.IsNullOrEmpty(item.KondisiAkhirPath))
        {
            string fullPath = Path.Combine(_storageService.ImagePath, item.KondisiAkhirPath);
            if (File.Exists(fullPath))
            {
                imgKondisiAkhir.Source = new BitmapImage(new Uri(fullPath));
                _currentKondisiAkhirPath = fullPath;
            }
        }
        
        txtIdBarang.IsEnabled = false;
        btnHapus.IsEnabled = true;
        _isEditMode = true;
    }

    private async void BtnSimpan_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateInputs())
            return;

        try
        {
            var item = GetItemFromInputs();
            
            if (_isEditMode)
            {
                // Update existing item
                var existingItem = _daftarBarang.FirstOrDefault(i => i.IdBarang == item.IdBarang);
                if (existingItem != null)
                {
                    int index = _daftarBarang.IndexOf(existingItem);
                    _daftarBarang[index] = item;
                }
            }
            else
            {
                // Add new item
                _daftarBarang.Add(item);
            }
            
            await _storageService.SaveItemsAsync(_daftarBarang);
            
            // Refresh the DataGrid
            dgBarang.ItemsSource = null;
            dgBarang.ItemsSource = _daftarBarang;
            
            if (!_isEditMode)
            {
                MsgBox.Show("Data berhasil disimpan!", "Informasi", MessageBoxButton.OK, MessageBoxImage.Information);
                // Select the newly added/edited item
                dgBarang.SelectedItem = _daftarBarang.FirstOrDefault(i => i.IdBarang == item.IdBarang);
                _isEditMode = true;
                txtIdBarang.IsEnabled = false;
                btnHapus.IsEnabled = true;
                
                // Mengaktifkan tombol pilih foto setelah data disimpan
                btnPilihFoto.IsEnabled = true;
                btnPilihFotoKondisiAwal.IsEnabled = true;
                btnPilihFotoKondisiAkhir.IsEnabled = true;
            }
            else
            {
                MsgBox.Show("Data berhasil diperbarui!", "Informasi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MsgBox.Show($"Terjadi kesalahan: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnBaru_Click(object sender, RoutedEventArgs e)
    {
        if (dgBarang.SelectedItem != null)
        {
            var result = MsgBox.Show("Apakah Anda yakin ingin membuat data baru? Data yang belum disimpan akan hilang.", "Konfirmasi", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.No)
            {
                return;
            }
        }
        ClearInputs();
        btnPilihFoto.IsEnabled = false;
        btnPilihFotoKondisiAwal.IsEnabled = false;
        btnPilihFotoKondisiAkhir.IsEnabled = false;
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
                    LoadData();
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
        _selectedItem = dgBarang.SelectedItem as Item;
        if (_selectedItem != null)
        {
            SetInputsFromItem(_selectedItem);
            btnPilihFoto.IsEnabled = true;
            btnPilihFotoKondisiAwal.IsEnabled = true;
            btnPilihFotoKondisiAkhir.IsEnabled = true;
        }
        else
        {
            ClearInputs();
            btnPilihFoto.IsEnabled = false;
            btnPilihFotoKondisiAwal.IsEnabled = false;
            btnPilihFotoKondisiAkhir.IsEnabled = false;
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
        if (decimal.TryParse(txtHargaBarang.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal harga))
        {
            txtHargaBarang.Text = harga.ToString("C0", CultureInfo.CurrentCulture);
        }
        else if (!string.IsNullOrWhiteSpace(txtHargaBarang.Text))
        {
            // Jika input tidak valid, kosongkan atau berikan pesan
            // txtHargaBarang.Text = string.Empty; // Option 1: Kosongkan
            // MessageBox.Show("Format harga tidak valid.", "Error"); // Option 2: Tampilkan pesan
        }
    }

    private void TxtHargaBarang_GotFocus(object sender, RoutedEventArgs e)
    {
        if (decimal.TryParse(txtHargaBarang.Text, NumberStyles.Currency, CultureInfo.CurrentCulture, out decimal harga))
        {
            txtHargaBarang.Text = harga.ToString("N0", CultureInfo.InvariantCulture);
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

                    if (backupItems != null)
                    {
                        // Clear current images from UI
                        imgBarang.Source = null;
                        imgKondisiAwal.Source = null;
                        imgKondisiAkhir.Source = null;
                        
                        // Force garbage collection to release file handles
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        
                        // Salin data JSON
                        await _storageService.SaveItemsAsync(backupItems);
                        
                        // Salin gambar jika folder gambar backup ada
                        if (Directory.Exists(backupImagesPath))
                        {
                            try
                            {
                                // Hapus semua file gambar lama
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
                                
                                // Salin semua gambar dari backup
                                foreach (string imageFile in Directory.GetFiles(backupImagesPath))
                                {
                                    string fileName = Path.GetFileName(imageFile);
                                    string destImagePath = Path.Combine(_storageService.ImagePath, fileName);
                                    File.Copy(imageFile, destImagePath);
                                }
                            }
                            catch (Exception ex)
                            {
                                MsgBox.Show($"Terjadi kesalahan saat memproses file gambar: {ex.Message}\nProses restore data tetap dilanjutkan.", 
                                    "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                        }

                        LoadData();
                        ClearInputs();

                        MsgBox.Show("Data dan gambar berhasil dimuat dari backup.", 
                                      "Load Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MsgBox.Show("File backup tidak valid atau kosong.", 
                                      "Load Gagal", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MsgBox.Show($"Terjadi kesalahan saat load backup: {ex.Message}", 
                                  "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private void BtnPilihFoto_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new Win32.OpenFileDialog
        {
            Filter = "Image files (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png",
            Title = "Pilih foto barang"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            _currentFotoPath = openFileDialog.FileName;
            imgBarang.Source = new BitmapImage(new Uri(_currentFotoPath));
        }
    }

    private void BtnPilihFotoKondisiAwal_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new Win32.OpenFileDialog
        {
            Filter = "Image files (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png",
            Title = "Pilih Foto Kondisi Awal"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            _currentKondisiAwalPath = openFileDialog.FileName;
            imgKondisiAwal.Source = new BitmapImage(new Uri(_currentKondisiAwalPath));
        }
    }

    private void BtnPilihFotoKondisiAkhir_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new Win32.OpenFileDialog
        {
            Filter = "Image files (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png",
            Title = "Pilih Foto Kondisi Akhir"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            _currentKondisiAkhirPath = openFileDialog.FileName;
            imgKondisiAkhir.Source = new BitmapImage(new Uri(_currentKondisiAkhirPath));
        }
    }
}