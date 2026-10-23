namespace Cadastro_Curriculos.Services;

public class FalhaLeituraPdfException(string message, Exception? innerException = null)
    : Exception(message, innerException);