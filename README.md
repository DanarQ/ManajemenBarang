# Aplikasi Manajemen Barang

Aplikasi desktop sederhana untuk manajemen barang (inventaris) menggunakan C# dan WPF.

## Fitur

- **Menyimpan Data**: Menambahkan data barang baru
- **Mengedit Data**: Mengubah data barang yang sudah ada
- **Menghapus Data**: Menghapus data barang
- **Menampilkan Data**: Menampilkan daftar barang dalam bentuk tabel
- **Pencarian Data**: Mencari barang berdasarkan beberapa kriteria

## Informasi yang Disimpan

- No ID Barang
- Nama Barang
- Merek Barang
- Harga Barang
- Tanggal Perolehan
- Tanggal Peminjaman
- Nama Pengguna
- Keterangan Barang

## Cara Menggunakan Aplikasi

### Menjalankan Aplikasi

1. Buka folder aplikasi dalam Visual Studio atau editor lainnya.
2. Buka terminal dan jalankan perintah `dotnet run` di folder ManajemenBarang.
3. Aplikasi akan terbuka dengan tampilan utama.

### Menambahkan Data Barang

1. Isi semua field pada form di sisi kiri aplikasi (No ID Barang, Nama Barang, dll.)
2. Klik tombol "Simpan" untuk menyimpan data.
3. Data yang tersimpan akan muncul pada tabel di sisi kanan.

### Mengedit Data Barang

1. Pilih barang yang ingin diedit dari tabel di sisi kanan.
2. Data barang akan otomatis diisi pada form di sisi kiri.
3. Ubah data yang diinginkan.
4. Klik tombol "Simpan" untuk menyimpan perubahan.

### Menghapus Data Barang

1. Pilih barang yang ingin dihapus dari tabel di sisi kanan.
2. Klik tombol "Hapus".
3. Konfirmasi penghapusan data.

### Mencari Data Barang

1. Masukkan kata kunci pencarian pada kolom pencarian di atas tabel.
2. Hasil pencarian akan otomatis ditampilkan di tabel.
3. Pencarian dapat dilakukan berdasarkan No ID, Nama Barang, Merek, Nama Pengguna, atau Keterangan.

## Penyimpanan Data

Aplikasi menyimpan data secara lokal dalam format JSON di folder:
`%LocalAppData%\ManajemenBarang\items.json`

## Persyaratan Sistem

- Windows 7 atau lebih baru
- .NET 9.0 atau lebih baru

## Pengembangan

Aplikasi ini dikembangkan menggunakan:

- C# sebagai bahasa pemrograman
- WPF (Windows Presentation Foundation) untuk antarmuka pengguna
- JSON untuk penyimpanan data lokal

note( Dalam pembuatan aplikasi ada campur tangan AI )
