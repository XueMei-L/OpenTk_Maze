using OpenTK.Mathematics;
using Optional;
using Optional.Unsafe;

public class Collision {

  public static bool CheckEB(Actor actorE, Actor actorB)
  {
    if(! actorE.CollisionGeometry.HasValue || ! actorB.CollisionGeometry.HasValue )
        return false;

    CollisionBox cBox= (CollisionBox) actorB.CollisionGeometry.ValueOrFailure("Checking collision in actor without collision geometry");
    CollisionEllipsoid cEllipsoid= (CollisionEllipsoid) actorE.CollisionGeometry.ValueOrFailure("Checking collision in actor without collision geometry");
   
    // Check each corner 
    Vector3[] corners=cBox.corners;
    foreach(Vector3 corner in corners)
    {
        if(cEllipsoid.EllipsoidContainsVertex(corner))
            return true;
        
    }

    // Check each edge
    Tuple<int,int>[] edges= cBox.edges;
    foreach(Tuple<int,int> edge in edges)
    {
	    Vector3 p1=cBox.corners[edge.Item1];
	    Vector3 p2=cBox.corners[edge.Item2];
	    if(cEllipsoid.EllipsoidCrossEdge(p1,p2))
		    return true;
    }
    
    // Check each face
    CollisionBox.Face[] faces=cBox.faces;
    foreach(CollisionBox.Face face in faces)
    {
	    //  First three of the face corners
	    //  Assuming first are one side and
	    //  last two are the other side of the quad
	    Vector3[] p=new Vector3[] {cBox.corners[face.corners[0]],
		    cBox.corners[face.corners[1]],
		    cBox.corners[face.corners[2]]};
	    // Face normal
	    Vector3 U=face.normal;
	    if(cEllipsoid.EllipsoidCrossFace(p,U))
		    return true;
    }



    return false;
  }

}
