namespace DriverTrackingService.Helpers;

public static class DistanceCalculator
{
    /// <summary>
    /// İki coğrafi koordinat arasındaki mesafeyi kilometre (km) cinsinden hesaplar.
    /// </summary>
    public static double CalculateWithHaversine(double lat1, double lon1, double lat2, double lon2)
    {
        var R = 6371; // Dünyanın yarıçapı (km)

        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return R * c; // Kilometre cinsinden mesafe
    }

    private static double ToRadians(double angle)
    {
        return (Math.PI / 180) * angle;
    }
}