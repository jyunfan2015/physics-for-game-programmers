using System;

public class SimpleRocket : ODE
{
  //  Fields that define the rocket
  private int numberOfEngines;
  private double seaLevelThrustPerEngine;
  private double vacuumThrustPerEngine;
  private double rocketDiameter;
  private double cd;
  private double initialMass;
  private double burnTime;
  private USatm76 air;

  public SimpleRocket(double x0, double y0, double z0, double vx0, 
                      double vy0, double vz0, double time, 
                      double initialMass, double massFlowRate,
               int numberOfEngines, double seaLevelThrustPerEngine, 
               double vacuumThrustPerEngine, double rocketDiameter, 
               double cd, double theta, double omega, double burnTime) :
                 base(10) {

    //  Load the initial values into the s field 
    //  and q array from the ODE class.
    this.S = time;
    SetQ(vx0,0);
    SetQ(x0, 1);
    SetQ(vy0,2);
    SetQ(y0, 3);
    SetQ(vz0,4);
    SetQ(z0, 5);
    SetQ(massFlowRate,6);
    SetQ(initialMass, 7);
    SetQ(omega, 8);  //  d(theta)/dt in radians/s
    SetQ(theta, 9);  //  pitch angle in radians

    //  Initialize the values of the fields declared
    //  in the SimpleRocket class.
    this.numberOfEngines = numberOfEngines;
    this.seaLevelThrustPerEngine = seaLevelThrustPerEngine;
    this.vacuumThrustPerEngine = vacuumThrustPerEngine;
    this.rocketDiameter = rocketDiameter;
    this.cd = cd;
    this.initialMass = initialMass;
    this.burnTime = burnTime;

    //  Initialize the atmosphere model.
    air = new USatm76(z0);
  }

  //  These methods return the location, velocity, 
  //  and time values
  public double GetVx() {
    return GetQ(0);
  }

  public double GetVy() {
    return GetQ(2);
  }

  public double GetVz() {
    return GetQ(4);
  }

  public double GetX() {
    return GetQ(1);
  }

  public double GetY() {
    return GetQ(3);
  }

  public double GetZ() {
    return GetQ(5);
  }

  public double GetMassFlowRate() {
    return GetQ(6);
  }

  public double GetMass() {
    return GetQ(7);
  }

  public double GetOmega() {
    return GetQ(8);
  }

  public double GetTheta() {
    return GetQ(9);
  }

  public double GetTime() {
    return this.S;
  }

  //  These properties access the values of the fields
  //  declared in the class
  public int NumberOfEngines {
    get {
      return numberOfEngines;
    }
  }

  public double SeaLevelThrustPerEngine {
    get {
      return seaLevelThrustPerEngine;
    }
  }

  public double VacuumThrustPerEngine {
    get {
      return vacuumThrustPerEngine;
    }
  }

  public double RocketDiameter {
    get {
      return rocketDiameter;
    }
  }

  public double Cd {
    get {
      return cd;
    }
  }

  public double InitialMass {
    get {
      return initialMass;
    }
  }

  public double BurnTime {
    get {
      return burnTime;
    }
  }

  //  This method updates the velocity and location
  //  of the rocket using a 4th order Runge-Kutta
  //  solver to integrate the equations of motion.
  public void UpdateLocationAndVelocity(double dt) {
    ODESolver.RungeKutta4(this, dt);
  }

  //  The GetRightHandSide() method returns the right-hand
  //  sides of the equations of motion for the rocket.
  //  q[0] = vx = dx/dt
  //  q[1] = x
  //  q[2] = vy = dy/dt
  //  q[3] = y
  //  q[4] = vz = dz/dt
  //  q[5] = z
  //  q[6] = mass flow rate = dm/dt
  //  q[7] = mass
  //  q[8] = omega = d(theta)/dt
  //  q[9] = theta
  public override double[] GetRightHandSide(double s, double[] q, 
                              double[] deltaQ, double ds,
                              double qScale) {
    double[] dQ = new double[10];
    double[] newQ = new double[10];

    //  Compute the intermediate values of the 
    //  location and velocity components.
    for(int i=0; i<10; ++i) {
      newQ[i] = q[i] + qScale*deltaQ[i];
    }

    //  Assign convenenience variables to the intermediate 
    //  values of the locations and velocities.
    double vx = newQ[0];
    double vy = newQ[2];
    double vz = newQ[4];
    double vtotal = Math.Sqrt(vx*vx + vy*vy + vz*vz);
    double x = newQ[1];
    double y = newQ[3];
    double z = newQ[5];
    double massFlowRate = newQ[6];
    double mass = newQ[7];
    double omega = newQ[8];
    double theta = newQ[9];

    //  Update the values of pressure, density, and
    //  temperature based on the current altitude.
    air.UpdateConditions(z);
    double pressure = air.Pressure;
    double density = air.Density;

    //  Compute the thrust per engine and total thrust
    double pressureRatio = pressure/101325.0;
    double thrustPerEngine = vacuumThrustPerEngine - 
       (vacuumThrustPerEngine - seaLevelThrustPerEngine)*pressureRatio;
    double thrust = numberOfEngines*thrustPerEngine;

    //  Compute the drag force based on the frontal area
    //  of the rocket.
    double area = 0.25*Math.PI*rocketDiameter*rocketDiameter;
    double drag = 0.5*cd*density*vtotal*vtotal*area;

    //  Compute the gravitational acceleration
    //  as a function of altitude
    double re = 6356766.0;  // radius of the Earth in meters.
    double g = 9.80665*re*re/Math.Pow(re+z,2.0);

    //  For this simulation, lift will be assumed to be zero.
    double lift = 0.0;

    //  Compute the force components in the x- and z-directions.
    //  The rocket will be assumed to be traveling in the x-z plane.
    double Fx = (thrust - drag)*Math.Cos(theta) - lift*Math.Sin(theta);
    double Fz = (thrust - drag)*Math.Sin(theta) + lift*Math.Cos(theta) -
                mass*g;

    //  Load the right-hand sides of the ODE's
    dQ[0] = ds*(Fx/mass);
    dQ[1] = ds*vx;
    dQ[2] = 0.0;   //  y-component of accleration = 0
    dQ[3] = 0.0;
    dQ[4] = ds*(Fz/mass);
    dQ[5] = ds*vz;
    dQ[6] = 0.0;   //  mass flow rate is constant
    dQ[7] = -ds*(massFlowRate*numberOfEngines);
    dQ[8] = 0.0;   //  d(theta)/dt is constant
    dQ[9] = ds*omega;

    return dQ;
  }
}
