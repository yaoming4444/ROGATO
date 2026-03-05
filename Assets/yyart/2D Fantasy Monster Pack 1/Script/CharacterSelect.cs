using System.Collections.Generic;
using UnityEngine;

namespace yyart.yyart._2D_Fantasy_Monster_Pack_1.Script
{
  public class CharacterSelect : MonoBehaviour
  {
    private List<GameObject> characters;
    // Default Index of the characters
    private int selection = 0;

    public Animator anim;

    private void Start()
    {
      characters = new List<GameObject>();
      foreach (Transform t in transform)
      {
        characters.Add(t.gameObject);
        t.gameObject.SetActive(false);
      }
      characters[selection].SetActive(true);
      anim = characters[selection].GetComponent<Animator>();
    }

//public void Select()
    public void Select(int index)
    {
      //next
      if(index == 1){
        characters[selection].SetActive(false);
        selection++;
        if(selection >= characters.Count)
          selection = 0;
      }
      //previous
      if(index == 5){
        characters[selection].SetActive(false);
        selection--;
        if(selection <= 0)
          selection = characters.Count -1;
      }

      characters[selection].SetActive(true);
      anim = characters[selection].GetComponent<Animator>();
    }

    public void Animate(int index)
    {
      if(index == 0)//attack
      {
        anim.Play("Attack", 0, 0.0f);
      } 
      if(index == 1)//walk
      {
        anim.Play("Walk", 0, 0.0f);
      } 
      if(index == 3)//hit
      {
        anim.Play("Hit", 0, 0.0f);
      } 
      if(index == 4)//death
      {
        anim.Play("Death", 0, 0.0f);
      } 
      if(index == 5)//idle
      {
        anim.Play("Idle", 0, 0.0f);
      }
    }
  }
}
