namespace ProceduralCityGenerator.Scripts.DataStructure;

public struct StreamLinesParams
{
    public StreamLinesParams SetSep(float sep)
    {
        this.sep = sep;
        return this;
    }

    public StreamLinesParams SetLookahead(float lookahead)
    {
        this.lookahead = lookahead;
        return this;
    }

    public static StreamLinesParams Default => new()
    {
        sep = 100f,
        test = 50f,
        step = 1,
        lookahead = 500,
        circleJoin = 5000,
        joinAngle = 0.1f,
        pathIterations = 500,
        seedTries = 300,
        simplifyTolerance = 0.0125f,
        colliderEarly = 0
    };

    public float sep;
    public float test;
    public float step;
    public float lookahead;
    public float circleJoin;
    public float joinAngle;
    public float pathIterations;
    public float seedTries;
    public float simplifyTolerance;
    public float colliderEarly;
}