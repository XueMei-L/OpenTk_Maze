using OpenTK.Mathematics;
using System;

class CollisionEllipsoid : CollisionGeometry {

    public Matrix3 M {get; private set;}
    private Matrix3 M_inv;
    public Matrix3 V {get; private set;}
    public CollisionEllipsoid() : base() {
        CalculateVM();
    }
    public CollisionEllipsoid(Vector3 center, Vector3 dim ) : base(center,dim) {
        CalculateVM();
    }

    private void CalculateVM()
    {
        //Rotation Matrix Transposed
        Vector3 u0 = parameters.uX;
        Vector3 u1 = parameters.uY;
        Vector3 u2 = parameters.uZ;
        Vector3 dimensions = parameters.dimensions;
        Matrix3 Vt=new Matrix3(u0,u1,u2);
        
        float d0=1.0f/dimensions.X;
        float d1=1.0f/dimensions.Y;
        float d2=1.0f/dimensions.Z;
        Matrix3 D2 = new Matrix3(d0*d0,0.0f,0.0f,0.0f,d1*d1,0.0f,0.0f,0.0f,d2*d2);
        V=Matrix3.Transpose(Vt);
        M= V*D2*Vt;
	M_inv=Matrix3.Invert(M);
        


    }

    public override void Transform(in Matrix4 Model)
    {
	    base.Transform(Model);
	    CalculateVM();
    }




    public bool EllipsoidContainsVertex(in Vector3 p)
    {
        Vector3 pmk=p - parameters.center;
        return (Vector3.Dot(pmk*M,pmk)<=1);

    }


    public bool EllipsoidCrossEdge(in Vector3 p0, in Vector3 p1)
    {
	    // This works if p1 and p2 are outside the ellipsoid
	
	    Vector3 p10=p1-p0;
	    Vector3 p0k=p0-parameters.center;
	    float q0=Vector3.Dot(p0k*M,p0k)-1;
	    float q1=2*Vector3.Dot(p0k*M,p10);
	    float q2=Vector3.Dot(p10*M,p10);
	    if((q1*q1-4*q0*q2)>=0)
	    {
		    float s=(float)Math.Sqrt(q1*q1-4*q2*q0);
		    float t0=(-q1-s)/(2*q2);
		    float t1=(-q1+s)/(2*q2);
		    // overlap with [0,1]
		    if(t1<0 || t0>1)
			    return false;
		    else
			    return true;
		    
	    }
	    else
		    return false;



    }

    public bool EllipsoidCrossFace(in Vector3[] corners, in Vector3 U)
    {
	    Vector3 p0=corners[0];
	    Vector3 p1=corners[1];
	    Vector3  p2=corners[2];
	    // Check for plane instersection
	    float p_proj=Vector3.Dot(U,p0-parameters.center);
	    float elip_proj=(float) MathHelper.Sqrt(Vector3.Dot(U*M_inv,U));
	    if ( MathHelper.Abs(p_proj)<elip_proj)
	    {
		    // Ellipsoid insertect plane, now check
		    // for elipsoid center inside face
		    // Using ellipsoid local coordinates
		    Vector3 p0e=(p0-parameters.center)*V;
		    Vector3 p1e=(p1-parameters.center)*V;
		    Vector3 p2e=(p2-parameters.center)*V;
		    Vector3 Ue=U*V;
		    //Plane equation: Ax+By+Cz+D=0
		    //A=Uex B=Uey C=Uez 
		    float D=Vector3.Dot(-Ue,p0e);


		    // Get point
		    Vector3 p = -D * Ue;
		    //Console.WriteLine($"Face {U} Center {p}"); 
		    //Console.WriteLine($"p0e {p0e} p1e {p1e} p2e {p2e}");

		    // Check if the point is inside the face
		    // p0e <= p <= p1e && p1e<= p <= p2e
		    Vector3 u10=Vector3.NormalizeFast(p1e-p0e);
                    float t1=Vector3.Dot((p0e-p),u10);
                    float t2=Vector3.Dot((p1e-p),u10);
		    if(t1*t2>0)
			    return false;
		    Vector3 u21=Vector3.NormalizeFast(p2e-p1e);
		    t1=Vector3.Dot((p1e-p),u21);
                    t2=Vector3.Dot((p2e-p),u21);
		    if(t1*t2>0)
			    return false;
		    return true;
	    }
	return false;


    }

}
