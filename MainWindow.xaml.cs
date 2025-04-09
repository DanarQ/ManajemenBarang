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

    public MainWindow()
    {
        InitializeComponent();
        _storageService = new StorageService();

        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _backupFolderPath = Path.Combine(appDataPath, "ManajemenBarang", "Backups");
        Directory.CreateDirectory(_backupFolderPath);

        LoadData();
        ClearInputs();
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
            MessageBox.Show($"Terjadi kesalahan saat memuat data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
        
        _selectedItem = null;
        _isEditMode = false;
        txtIdBarang.IsEnabled = true;
        btnHapus.IsEnabled = false;
    }

    private bool ValidateInputs()
    {
        if (string.IsNullOrWhiteSpace(txtIdBarang.Text))
        {
            MessageBox.Show("No ID Barang harus diisi!", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
            txtIdBarang.Focus();
            return false;
        }

        if (!_isEditMode && _daftarBarang.Any(item => item.IdBarang == txtIdBarang.Text))
        {
            MessageBox.Show($"ID Barang '{txtIdBarang.Text}' sudah digunakan. Harap gunakan ID lain.", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
            txtIdBarang.Focus();
            return false;
        }
        
        if (!int.TryParse(txtIdBarang.Text, out _))
        {
            MessageBox.Show("No ID Barang harus berupa angka!", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
            txtIdBarang.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(txtNamaBarang.Text))
        {
            MessageBox.Show("Nama Barang harus diisi!", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
            txtNamaBarang.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(txtMerekBarang.Text))
        {
            MessageBox.Show("Merek Barang harus diisi!", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
            txtMerekBarang.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(txtHargaBarang.Text))
        {
            MessageBox.Show("Harga Barang harus diisi!", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
            txtHargaBarang.Focus();
            return false;
        }
        else if (!decimal.TryParse(txtHargaBarang.Text, NumberStyles.Currency, CultureInfo.CurrentCulture, out _) &&
                 !decimal.TryParse(txtHargaBarang.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
        {
            MessageBox.Show("Harga Barang harus diisi dengan angka yang valid!", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
            txtHargaBarang.Focus();
            return false;
        }

        if (dpTanggalPerolehan.SelectedDate == null)
        {
            MessageBox.Show("Tanggal Perolehan harus dipilih!", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
            dpTanggalPerolehan.Focus();
            return false;
        }

        if (dpTanggalPinjam.SelectedDate == null)
        {
            MessageBox.Show("Tanggal Pinjam harus dipilih!", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
            dpTanggalPinjam.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(txtNamaPengguna.Text))
        {
            MessageBox.Show("Nama Pengguna harus diisi!", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
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
        
        return new Item
        {
            IdBarang = txtIdBarang.Text,
            NamaBarang = txtNamaBarang.Text,
            MerekBarang = txtMerekBarang.Text,
            HargaBarang = hargaBarang,
            TanggalPerolehan = dpTanggalPerolehan.SelectedDate ?? DateTime.Now,
            TanggalPinjam = dpTanggalPinjam.SelectedDate ?? DateTime.Now,
            NamaPengguna = txtNamaPengguna.Text,
            KeteranganBarang = txtKeteranganBarang.Text
        };
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
        
        txtIdBarang.IsEnabled = false;
        btnHapus.IsEnabled = true;
        _isEditMode = true;
    }

    private async void BtnSimpan_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateInputs()) return;

        try
        {
            var item = GetItemFromInputs();
            bool success;

            if (_isEditMode)
            {
                success = await _storageService.UpdateItemAsync(item);
                if (success)
                {
                    MessageBox.Show("Data barang berhasil diperbarui!", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
                    _selectedItem = item;
                }
                else
                {
                    MessageBox.Show("Gagal memperbarui data barang!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            else
            {
                success = await _storageService.AddItemAsync(item);
                if (success)
                {
                    MessageBox.Show("Data barang berhasil disimpan!", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
                    ClearInputs();
                }
                else
                {
                    MessageBox.Show("ID Barang sudah ada, gunakan ID yang lain!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            LoadData();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Terjadi kesalahan: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnBaru_Click(object sender, RoutedEventArgs e)
    {
        dgBarang.UnselectAll();
        
        _selectedItem = null;
        _isEditMode = false;
        
        txtNamaBarang.Text = string.Empty;
        txtMerekBarang.Text = string.Empty;
        txtHargaBarang.Text = string.Empty;
        dpTanggalPerolehan.SelectedDate = DateTime.Now;
        dpTanggalPinjam.SelectedDate = DateTime.Now;
        txtNamaPengguna.Text = string.Empty;
        txtKeteranganBarang.Text = string.Empty;
        
        txtIdBarang.Text = GetNextId().ToString();
        txtIdBarang.IsEnabled = true;
        
        btnHapus.IsEnabled = false;
        
        txtNamaBarang.Focus();
    }

    private async void BtnHapus_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedItem == null)
        {
            MessageBox.Show("Silakan pilih barang yang akan dihapus!", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var result = MessageBox.Show($"Anda yakin ingin menghapus barang dengan ID {_selectedItem.IdBarang}?", 
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
                    MessageBox.Show("Data barang berhasil dihapus!", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
                    ClearInputs();
                    LoadData();
                }
                else
                {
                    MessageBox.Show("Gagal menghapus data barang!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Terjadi kesalahan: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void DgBarang_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedItem = dgBarang.SelectedItem as Item;
        if (_selectedItem != null)
        {
            SetInputsFromItem(_selectedItem);
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
            string fileNamePattern = "backup_*_*.json";
            var existingBackups = Directory.GetFiles(_backupFolderPath, fileNamePattern);
            if (existingBackups.Length > 0)
            {
                var lastBackupNumber = existingBackups
                    .Select(f => Path.GetFileNameWithoutExtension(f).Split('_')[1])
                    .Where(n => int.TryParse(n, out _))
                    .Select(int.Parse)
                    .OrderByDescending(n => n)
                    .FirstOrDefault();
                backupNumber = lastBackupNumber + 1;
            }

            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string backupFileName = $"backup_{backupNumber}_{timestamp}.json";
            string backupFilePath = Path.Combine(_backupFolderPath, backupFileName);

            string sourceFilePath = _storageService.FilePath;

            if (File.Exists(sourceFilePath))
            {
                File.Copy(sourceFilePath, backupFilePath);
                MessageBox.Show($"Data berhasil di-backup ke:\n{backupFilePath}", "Backup Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("File data utama (items.json) tidak ditemukan. Backup dibatalkan.", "Backup Gagal", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Terjadi kesalahan saat backup: {ex.Message}", "Backup Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnLoad_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog
        {
            InitialDirectory = _backupFolderPath,
            Filter = "JSON backup files (*.json)|*.json",
            Title = "Pilih File Backup untuk Dimuat"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            string selectedBackupPath = openFileDialog.FileName;

            var result = MessageBox.Show("Memuat backup akan menimpa data saat ini.\nAnda yakin ingin melanjutkan?",
                                         "Konfirmasi Load Backup",
                                         MessageBoxButton.YesNo,
                                         MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    string json = await File.ReadAllTextAsync(selectedBackupPath);
                    var backupItems = JsonSerializer.Deserialize<List<Item>>(json);

                    if (backupItems != null)
                    {
                        await _storageService.SaveItemsAsync(backupItems);

                        LoadData();
                        ClearInputs();

                        MessageBox.Show("Data berhasil dimuat dari backup.", "Load Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("File backup tidak valid atau kosong.", "Load Gagal", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Terjadi kesalahan saat load backup: {ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}