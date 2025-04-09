using System;

namespace ManajemenBarang.Models
{
    public class Item
    {
        public required string IdBarang { get; set; }
        public required string NamaBarang { get; set; }
        public required string MerekBarang { get; set; }
        public decimal HargaBarang { get; set; }
        public DateTime TanggalPinjam { get; set; }
        public required string NamaPengguna { get; set; }
        public string? KeteranganBarang { get; set; }

        public Item()
        {
            TanggalPinjam = DateTime.Now;
        }

        public Item(string idBarang, string namaBarang, string merekBarang, decimal hargaBarang,
                    DateTime tanggalPinjam, string namaPengguna, string? keteranganBarang)
        {
            IdBarang = idBarang;
            NamaBarang = namaBarang;
            MerekBarang = merekBarang;
            HargaBarang = hargaBarang;
            TanggalPinjam = tanggalPinjam;
            NamaPengguna = namaPengguna;
            KeteranganBarang = keteranganBarang;
        }
    }
} 