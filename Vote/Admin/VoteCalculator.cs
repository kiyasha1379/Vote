public static class VoteCalculator
{
    // درصد آرا داوران طلایی از 80
    public static double CalculateGoldenRefereePercent(int votes, int totalVotes)
    {
        if (totalVotes == 0) return 0;
        double percent = ((double)votes / totalVotes) * 80;
        return Math.Round(percent, 2);
    }

    // درصد آرا داوران نقره‌ای از 10
    public static double CalculateSilverRefereePercent(int votes, int totalVotes)
    {
        if (totalVotes == 0) return 0;
        double percent = ((double)votes / totalVotes) * 10;
        return Math.Round(percent, 2);
    }

    // درصد آرا کاربران عادی از 10
    public static double CalculateUserPercent(int votes, int totalVotes)
    {
        if (totalVotes == 0) return 0;
        double percent = ((double)votes / totalVotes) * 10;
        return Math.Round(percent, 2);
    }
}
