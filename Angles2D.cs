using OpenTK.Mathematics;

public class Angles2D {

        public double Yaw {get; set;}
        public double Pitch{get; set;}

        // Constructors

        public Angles2D(){
            Yaw=0.0;
            Pitch=0.0;
        }

        public Angles2D(double yaw, double pitch){

            Yaw=clampYaw(yaw);
            Pitch = clampPitch(pitch % 360.0);

        }

        public Angles2D(Angles2D ang){

            Yaw = ang.Yaw;
            Pitch = ang.Pitch;

        }

        // Operators +,-

        public static Angles2D operator+(Angles2D bA1,Angles2D bA2){
            double yaw,pitch;
            yaw = bA1.Yaw + bA2.Yaw;
            yaw=clampYaw(yaw);
               
            pitch = bA1.Pitch + bA2.Pitch;
            pitch = clampPitch(pitch % 360.0);

            return new Angles2D(yaw,pitch);
        }
        public static Angles2D operator-(Angles2D bA1,Angles2D bA2){
            double yaw,pitch;
            yaw = bA1.Yaw - bA2.Yaw;
            yaw  = clampYaw(yaw);
            pitch = bA1.Pitch - bA2.Pitch;
            pitch = clampPitch(pitch % 360.0);
            return new Angles2D(yaw,pitch);
        }

        public static Angles2D operator*(double f, Angles2D bA)
            => new Angles2D(clampYaw(f*bA.Yaw),clampPitch(f*bA.Pitch));
        public static Angles2D operator*(Angles2D bA,double f)
            => f*bA;

        // Clamps 
        private static double clampPitch(double pitch){
            double lim=89.0f;
            if(pitch<-lim)
                return -lim;
            if(pitch>lim)
                return lim;
            return pitch;
        }

        private static double clampYaw(double yaw)
        {
            yaw  = yaw % 360.0;
            if(yaw>180)
                yaw=-(360.0-yaw);
            
            return yaw;

        }


    }