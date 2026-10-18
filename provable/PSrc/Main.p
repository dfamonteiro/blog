// The simplest P state machine: it starts, checks an invariant, and quiesces.
machine Main {
  start state Init {
    entry {
      assert(true);
    }
  }
}
