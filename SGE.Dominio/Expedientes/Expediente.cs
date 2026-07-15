namespace SGE.Dominio.Expedientes;

using System;
using SGE.Dominio.Tramites; 
using SGE.Dominio.Comun;
// Esta clase representa un expediente, que es una entidad con identidad propia y un ciclo de vida definido.
public class Expediente
{
    //Propiedades: Acceso publico para lectura (get), pero solo la clase puede modificar el atributo por el private
    public Guid Id { get; private set; } 
    public Caratula Caratula { get; private set; } = null!; //Value object q valida el texto
    public DateTime FechaCreacion { get; private set; } 
    public DateTime FechaUltimaModificacion { get; private set; } 
    public Guid UsuarioUltimoCambio { get; private set; } 
    public EstadoExpediente Estado { get; private set; }


    private Expediente() 
    {
        // Constructor vacío para que la persistencia arme el objeto
    }

    public Expediente(Caratula caratula, Guid idUsuario, DateTime fechaCreacion)
    : this(Guid.NewGuid(), caratula, fechaCreacion, fechaCreacion, idUsuario, EstadoExpediente.RecienIniciado)
    {
       //Llama al constructor privado con los parametros iniciales del alta. 
    }

    // Constructor privado centralizado --> Asigna y valida los datos del objeto
    private Expediente(Guid id, Caratula caratula, DateTime fechaCreacion, DateTime fechaUltimaModificacion, Guid usuarioUltimoCambio, EstadoExpediente estado)
    {
        if (id == Guid.Empty) 
            throw new DominioException("El ID del expediente no puede ser un Guid vacío.");
        
        if (usuarioUltimoCambio == Guid.Empty) 
            throw new DominioException("El usuario del último cambio no puede ser un Guid vacío.");

        if (fechaUltimaModificacion < fechaCreacion) 
            throw new DominioException("La fecha de última modificación no puede ser anterior a la fecha de creación.");

        if (fechaCreacion > DateTime.Now)
            throw new DominioException("La fecha de creación no puede estar en el futuro.");

        // Asignación segura luego de corroborar las posibles excepciones
        Id = id;
        Caratula = caratula ?? throw new DominioException("La carátula no puede ser nula.");
        FechaCreacion = fechaCreacion;
        FechaUltimaModificacion = fechaUltimaModificacion;
        UsuarioUltimoCambio = usuarioUltimoCambio;
        Estado = estado;
    }

    //Este metodo permite modificar la caratula del expediente, y al mismo tiempo actualiza el usuario que hizo el cambio y la fecha de modificación.
    //ACTUALIZACION: Agregamos validaciones solicitadas en las observaciones (ID FECHA y Caratula NO vacia)
    public void ModificarCaratula (Caratula nuevaCaratula, Guid idUsuario,DateTime fechaModificacion)
    {
        if (idUsuario == Guid.Empty) 
            throw new DominioException("El usuario que realiza el cambio no puede ser un Guid vacío.");
        if (fechaModificacion < this.FechaCreacion)
            throw new DominioException("La fecha de modificación no puede ser anterior a la fecha de creación del expediente.");
        this.Caratula = nuevaCaratula ?? throw new DominioException("La nueva carátula no puede ser nula.");
        this.UsuarioUltimoCambio = idUsuario;
        this.FechaUltimaModificacion = fechaModificacion;
    }

    //Este método actualiza el estado del expediente según la última etiqueta de trámite aplicada, y también actualiza el usuario que hizo el cambio y la fecha de modificación. 
    //Devuelve un booleano indicando si hubo un cambio de estado o no.
    public bool ActualizarEstado (EtiquetaTramite? ultimaEtiqueta, Guid idUsuario, DateTime fechaModificacion)
    {
        //NUEVAS VALIDACIONES AGREGADAS, IDEM MODIFICAR CARATULA
        if (idUsuario == Guid.Empty) throw new DominioException("El usuario no puede ser vacío.");
        if (fechaModificacion < this.FechaCreacion) throw new DominioException("La fecha de modificación es inválida.");


        // Guardamos el estado anterior para saber si realmente hubo un cambio al final
        EstadoExpediente estadoAnterior = this.Estado;

        // Aplicamos las reglas de negocio del enunciado
        switch (ultimaEtiqueta)
        {
            case EtiquetaTramite.PaseAEstudio:
                this.Estado = EstadoExpediente.ParaResolver;
                break;
            case EtiquetaTramite.Resolucion:
                this.Estado = EstadoExpediente.ConResolucion;
                break;
            case EtiquetaTramite.Notificacion:
                this.Estado = EstadoExpediente.EnNotificacion;
                break;
            case EtiquetaTramite.PaseAlArchivo:
                this.Estado = EstadoExpediente.Finalizado;
                break;
            default:
                // Si la etiqueta es EscritoPresentado, Despacho o nula, no indica cambio de estado automático.
                break;
        }

        // Si el estado cambió después del switch, actualizamos 
        if (this.Estado != estadoAnterior)
        {
            this.UsuarioUltimoCambio = idUsuario;
            this.FechaUltimaModificacion = fechaModificacion;
            return true; // Hubo cambio
        }

        return false; // No hubo cambio
    }

    //Este metodo permite cambiar el estado del expediente a cualquier otro
    public void CambiarEstado (EstadoExpediente nuevoEstado, Guid idUsuario,DateTime fechaModificacion)
    {
        //NUEVAS VALIDACIONES AGREGADAS, IDEM MODIFICAR CARATULA
        if (idUsuario == Guid.Empty) throw new DominioException("El usuario no puede ser vacío.");
        if (fechaModificacion < this.FechaCreacion) throw new DominioException("La fecha de modificación es inválida.");
        
        // Simplemente asignamos el nuevo estado enviado
        this.Estado = nuevoEstado;
        this.UsuarioUltimoCambio = idUsuario;
        this.FechaUltimaModificacion = fechaModificacion;
    }

    //Este método estático se utiliza para reconstruir un expediente a partir de sus propiedades, lo que es útil para la persistencia y recuperación de datos.
    //FACTORY METHOD.
    public static Expediente Reconstruir(Guid id, Caratula caratula, DateTime fechaCreacion, DateTime fechaUltimaModificacion, Guid usuarioUltimoCambio, EstadoExpediente estado)
    {
       // En lugar de usar un constructor vacío e inicializar a mano, llamamos al constructor centralizado
        return new Expediente(id, caratula, fechaCreacion, fechaUltimaModificacion, usuarioUltimoCambio, estado); 
    }
}
