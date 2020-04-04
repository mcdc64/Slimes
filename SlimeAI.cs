using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlimeAI : MonoBehaviour
{
    public Rigidbody rb; //the rigid body of the slime.
    public float movespeed; //the speed that the slime can move at
    public float rot_speed; //the speed the slime rotates at
    public float notice_dist; //the maximum radius within which the slime notices food, walls and predators
    public MeshCollider slime_collider; //the collider of the slime

    private float size; //the size of the slime. Related to how much energy it has left.
    private Vector3 cross_product; //used to make the slime run from predators
    private bool noticed; //tells if slime is noticing a predator
    private bool avoiding; //tells if slime is avoiding a wall

    private float closest_predator_dist; //distance to nearest predator - tells the slime which one it needs to focus on
    private GameObject active; //the thing the slime is focusing on right now. Can be food or a predator - predators take priority
    private float rand_rot; //amount by which the slime randomly rotates sometimes
    private int rand_rot_sign; //sign of this rotation - needed because the rotation routine only "fires" when the actual value of the rand_rot variable is positive.

    private string[] priorityQueue;
    private float retreat; //number that is set to 180 whenever the slime needs to do a U-turn.
    


    private Random rand = new Random();
    // Start is called before the first frame update
    void Start()
    {
        
        
        noticed = false;
        avoiding = false;
        retreat = 0;
        rand_rot = 0;
        closest_predator_dist = 10000;
        size = slime_collider.bounds.size.x;
        priorityQueue = new string[3] {"Wall","Food", "Mate"};
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        avoiding = false;
        if (rand_rot <= 0)
        {
            rand_rot = Random.Range(-40f, 40f); //allow the slime to make random rotations, without them all cancelling out.
            rand_rot_sign = (int)Mathf.Sign(rand_rot); //this random fluctuation is cancelled when the slime needs to avoid walls or predators.
            rand_rot = Mathf.Abs(rand_rot);
        }
        closest_predator_dist = 10000;
        GameObject[] predators = GameObject.FindGameObjectsWithTag("Predator");
        for (int i = 0; i < predators.Length; i++) {
            
            if(NoticesObject((transform.position-predators[i].transform.position).magnitude, transform.forward, predators[i]))
            {
                noticed = true;
                rand_rot = 0;
                if (closest_predator_dist> (transform.position - predators[i].transform.position).magnitude) {
                    active = predators[i];
                    closest_predator_dist = (transform.position - predators[i].transform.position).magnitude;
                }
            }
        }


        float[] cast = WallCast();
        if (cast[0] > 0 || (cast[1] > 0 && cast[1]<=size) || (cast[2] > 0 && cast[2]<=size))
        
        if (retreat > 0)
        {
            transform.Rotate(0.0f, rot_speed, 0.0f);
            retreat -= rot_speed;
            Debug.Log(retreat);
        }
        else
        {
            CastDecide(cast);
        }

        if (noticed && !avoiding)
        { //"if the player is close enough and the slime can see it, the slime should act on it"
          //if the y component of the cross product is positive, the slime should rotate to the right to get away from the player and vice versa for negative
            if (retreat <= 0)
            {
                RunAway(active);

            }
        }

        else
        {
            if (!avoiding && rand_rot > 0)
            {
                Debug.Log(gameObject.name +": Rotating");
                float rot_amount = Random.Range(0, rot_speed/5);
                transform.Rotate(0.0f, rot_amount*rand_rot_sign, 0.0f); //add some random fluctuation to the slime's motion
                rand_rot -= rot_amount;
            }
            rb.velocity = Forward2d() * (movespeed);
            
        }   
        if (transform.eulerAngles.x != 0) //if the slime is starting to rotate upwards due to collisions, set it back down
        {
            transform.eulerAngles = new Vector3(0,transform.eulerAngles.y,transform.eulerAngles.z);
        }
        if (transform.eulerAngles.z != 0)
        {
            transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, 0);
        }
        

    }

    Vector3 Forward2d() { //returns the component of the slime's forward vector that is in the xz plane. This stops the slime from flying off if it gets rotated upwards, e.g. by collisions.
        Vector3 forward_2d = new Vector3(transform.forward.x, 0.0f, transform.forward.z);
        forward_2d = forward_2d / forward_2d.magnitude;
        return forward_2d;
    }

    bool NoticesObject(float dist, Vector3 forwards, GameObject active) { //check if the slime can see or sense a particular object
        
        float angle = Vector3.SignedAngle(forwards, active.transform.position - transform.position, new Vector3(0, 1.0f, 0));
        if (dist < notice_dist)
        {
            if (Mathf.Abs(angle) < 75 * (notice_dist / (dist + 1)) || noticed) //if we're already running away from the player, don't want the slime
                                                                               //to stop noticing player just by turning its back
            {
                noticed = true;
                return true;
            }
        }
        noticed = false;
        return false;
    }

    void RunAway(GameObject active) //make the slime run away from predators
    {
        Vector3 active_pos = active.transform.position;
        Vector3 rel_position = active_pos - transform.position; //get the relative position to the object so the slime knows where to go

        float dist = rel_position.magnitude;
        cross_product = Vector3.Cross(transform.forward, rel_position); //get the cross product. This is 0 if the slime is facing parallel or antiparallel to the object.

        transform.Rotate(0.0f, -rot_speed * Mathf.Sign(cross_product.y), 0.0f, Space.World);
        rb.velocity = Forward2d() * (movespeed * (notice_dist + 3) / (dist + 3)); //the slime should try harder to get away as the object gets closer}
    }

    float[] WallCast() //check for walls and figure out how to avoid them by casting 3 rays
    {
        Debug.Log("Starting Cast");
        //cast three rays, one forwards, two at 30 degree angles forwards (using a quaternion to rotate).
        float[] dists = new float[3] { 0.0f, 0.0f, 0.0f }; //in order, the distance to the left hit, the center hit, and the right hit

        float angle = 30;
        Vector3 right_vector = transform.forward;
        Vector3 left_vector = transform.forward;
        Quaternion rot_quaternion = Quaternion.AngleAxis(angle, Vector3.up);
        


        RaycastHit hit;
        if(Physics.Raycast(transform.position, transform.forward,out hit, notice_dist))
        {
            
            if (hit.collider.CompareTag("Inanimate")) { //if the rigidBody is kinematic and therefore an unmoving obstacle (e.g. not food or another slime)
                dists[1] = hit.distance;
                
            }
        }
        if (Physics.Raycast(transform.position, Quaternion.Inverse(rot_quaternion) *transform.forward, out hit, notice_dist)) //use the rotation quaternion to cast a ray to the left
        {
            if (hit.collider.CompareTag("Inanimate"))
            {
                dists[0] = hit.distance;
            }
        }
        if (Physics.Raycast(transform.position, rot_quaternion*transform.forward, out hit, notice_dist)) //get the inverse to rotate to the right
        {
            if (hit.collider.CompareTag("Inanimate"))
            { //if the rigidBody is kinematic and therefore an obstacle (e.g. not food or another slime)
                dists[2] = hit.distance;
            }
        }
        return dists;
    }

    void CastDecide(float[] cast)
    {
        
        if (cast[1] > 0)
        { //if there is a non-zero distance to an obstacle, the slime needs to avoid it in some way.
            Debug.Log("Hit Center");
            if (cast[0] > 0 && cast[2] > 0) //if all three rays hit a wall, we have three options: move left, move right, or retreat altogether.
            {
                Debug.Log("Hit Left, Right");
                if (cast[0] < cast[1] && cast[1] < cast[2])
                { //if the wall is to the "left" of the slime, move right to avoid it
                    transform.Rotate(0.0f, rot_speed, 0.0f, Space.World);
                    avoiding = true;
                }
                else if (cast[2] < cast[1] && cast[1] < cast[0]) //vice versa
                {
                    transform.Rotate(0.0f, -rot_speed, 0.0f, Space.World);
                    avoiding = true;
                }
                else if (cast[2] == cast[1] && cast[1] == cast[0]) //if we approach the wall head on, should decide randomly which way is best.
                {
                    transform.Rotate(0.0f, rot_speed*((2*Random.Range(0,1))-1), 0.0f, Space.World);
                    avoiding = true;
                }
                else if (cast[2] < cast[1] && cast[0] < cast[1] && cast[2]<2*size &&cast[0]<2*size) //this means we are moving into a corner. Set a "counter" called retreat that makes the slime do a 180
                {
                    Debug.Log("Cornered");
                    retreat = 180f;
                    avoiding = true;
                }
                else if(cast[0]>cast[1] && cast[1] > cast[2]) //means we are approaching a convex object that curves away from us, e.g. a circle. Avoiding this is simple enough
                {
                    transform.Rotate(0.0f, rot_speed, 0.0f);
                    avoiding = true;
                }
                

            }
            else if (cast[0] > 0) {
                transform.Rotate(0.0f, rot_speed, 0.0f, Space.World);
                avoiding = true;
            }
            else if (cast[2] > 0)
            {
                transform.Rotate(0.0f, -rot_speed, 0.0f, Space.World);
                avoiding = true;
            }
        }
        else
        {
            

            if (cast[2] > 0 && cast[2] < size && cast[0] > 0 && cast[0] < size) //if there is a gap but it's too small for the slime to pass through, retreat
            {
                retreat = 180f;
                avoiding = true;
            }

            else if (cast[0] > 0 && cast[0]<size)
            { //if there is nothing in front but something to the left, will need to move right due to non-zero width of slime.
                transform.Rotate(0.0f, rot_speed, 0.0f, Space.World);
                avoiding = true;
            }

            else if (cast[2] > 0 && cast [2]<size)
            {
                Debug.Log("Avoiding");
                transform.Rotate(0.0f, -rot_speed, 0.0f, Space.World);
                avoiding = true;
            }
            
        }
    }

    void OnCollisionEnter(Collision collision) //need to stop weird things from happening when the slimes collide with walls. Achieve this by reflecting velocity (not natural, but easiest solution)
    {
        if (collision.collider.gameObject.CompareTag("Inanimate"))
        {
            Vector3 normal = collision.contacts[0].normal;
            rb.velocity = rb.velocity - (2*(Vector3.Dot(rb.velocity,normal)))*normal;
            transform.Rotate(0.0f, Vector3.SignedAngle(transform.forward, rb.velocity, Vector3.up),0.0f,Space.World);
            transform.position = new Vector3(transform.position.x, 0.0f, transform.position.z);
        }
    }

    void OnCollisionExit(Collision collision)
    {
        
    }
}