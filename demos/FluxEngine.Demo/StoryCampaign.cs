public sealed record StoryChapter(string Title, IReadOnlyList<string> Pages);

public sealed class StoryCampaign
{
    public const float ChapterLength = 1450f;
    public const float InitialSpawnX = 100f;

    public static IReadOnlyList<string> Prologue { get; } =
    [
        "The city's lights went out one by one. At the edge of the rooftops, Nova found a signal still blinking.",
        "The old beacon can wake the city, but its path is broken. Follow the signal, cross the dark, and bring everyone home.",
    ];

    public static IReadOnlyList<StoryChapter> Chapters { get; } =
    [
        new("THE QUIET ROOFTOPS",
        [
            "The rooftops used to be loud after sunset. Nova follows a trail of lanterns toward the river.",
            "A note is tied beneath one light: 'If you can see this, the beacon is still listening.'",
        ]),
        new("THE SUNKEN DISTRICT",
        [
            "Rain has filled the old streets below. Far across the water, a second signal answers.",
            "Nova remembers the keeper's promise: no one gets left behind while the light is on.",
        ]),
        new("THE LAST BEACON",
        [
            "The final climb is steep, and the signal is almost gone. Nova keeps moving anyway.",
            "At the summit, the beacon catches. One window lights up, then another, then the whole city.",
        ]),
    ];

    public static IReadOnlyList<string> Ending { get; } =
    [
        "By morning, the rooftops are bright again. People follow the lights home, carrying the signal farther than Nova ever could.",
        "The city is awake. The next story belongs to everyone who found their way back.",
    ];

    public int CompletedChapters { get; private set; }
    public float CheckpointX { get; private set; } = InitialSpawnX;
    public float SegmentStartX { get; private set; } = InitialSpawnX;
    public bool IsComplete => CompletedChapters >= Chapters.Count;

    public string CurrentChapterTitle
        => Chapters[Math.Min(CompletedChapters, Chapters.Count - 1)].Title;

    public float Distance(float playerX)
        => CompletedChapters * ChapterLength + MathF.Max(0f, playerX - SegmentStartX);

    public float ChapterProgress(float playerX)
        => Math.Clamp((playerX - SegmentStartX) / ChapterLength, 0f, 1f);

    public void RespawnAtCheckpoint()
    {
        SegmentStartX = CheckpointX;
    }

    public bool TryCompleteChapter(float playerX, out StoryChapter? nextChapter, out bool campaignComplete)
    {
        nextChapter = null;
        campaignComplete = false;

        if (IsComplete || playerX - SegmentStartX < ChapterLength)
            return false;

        CompletedChapters++;
        CheckpointX = playerX;
        SegmentStartX = playerX;
        campaignComplete = IsComplete;
        if (!campaignComplete)
            nextChapter = Chapters[CompletedChapters];

        return true;
    }

    public void RestoreProgress(int completedChapters, float checkpointX, float segmentStartX)
    {
        if (completedChapters < 0 || completedChapters > Chapters.Count)
            throw new ArgumentOutOfRangeException(nameof(completedChapters));
        if (!float.IsFinite(checkpointX) || checkpointX < InitialSpawnX)
            throw new ArgumentOutOfRangeException(nameof(checkpointX));
        if (!float.IsFinite(segmentStartX) || segmentStartX < checkpointX)
            throw new ArgumentOutOfRangeException(nameof(segmentStartX));

        CompletedChapters = completedChapters;
        CheckpointX = checkpointX;
        SegmentStartX = segmentStartX;
    }

    public void Reset()
    {
        CompletedChapters = 0;
        CheckpointX = InitialSpawnX;
        SegmentStartX = InitialSpawnX;
    }
}
