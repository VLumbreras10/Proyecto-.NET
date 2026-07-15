namespace SGE.Dominio.Comun;

// Esta clase hereda de Exception y se utiliza para representar excepciones específicas del dominio de la aplicación.
public class DominioException : Exception
{
    public DominioException(string mensaje) : base(mensaje) { }
}
/*
Basicamente lo q hace es heredar de la clase Exception
lo que hace es que cuando alguien haga un throw new DominioException("mensaje")
se crea una nueva instancia y gracias a : base(mensaje) se le pasa el mensaje al 
constructor de la clase base Exception, lo que permite que el mensaje de error 
se almacene y se pueda acceder a él más tarde.
*/