public final class StatusCheckStarter {

    static Activity myActivity;

    // Called From C# to get the Activity Instance
    public static void receiveActivityInstance(Activity tempActivity) {
        myActivity = tempActivity;
    }

    public static void StartCheckerService() {
        myActivity.startService(new Intent(myActivity, CheckService.class));
    }
}