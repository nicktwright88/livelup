public final class test {

	// Called From C# to get the Activity Instance
	public static void main (String[] args) {
		Class c = StatusCheckStarter.class;
		System.out.println("Package: "+c.getPackage()+"\nClass: "+c.getSimpleName()+"\nFull Identifier: "+c.getName());
	}
}