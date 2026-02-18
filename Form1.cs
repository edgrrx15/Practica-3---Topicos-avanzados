using Practica3.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
namespace Practica3
{
    public partial class Form1 : Form
    {
        //==================================================
        //Objetos fijos
        // ** Los objetos fijos son el botón primario y el temporizador, estos objetos
        //    no se clonan ni se eliminan, solo se manipulan sus propiedades para crear la interacción con en el formulario
        //==================================================
        private Button botonPRIMARIO = new Button();
        private Timer temporizador = new Timer();


        // === Constantes ==================================
        private const int TEMPORIZADOR_INTERVAL = 50;
        private const int VELOCIDAD_MAXIMA = 20;
        private const int VELOCIDAD_MINIMA = 5;
        private const int VELOCIDAD_INICIAL = 13;

        private const int MAX_FORMULARIOS = 15;
        private const int WIDTH_FORMULARIO = 800;
        private const int HEIGHT_FORMULARIO = 600;
        private const int MAX_WIDTH_FORMULARIO = 1000;
        private const int MAX_HEIGHT_FORMULARIO = 800;

        private const int MAX_TAMANO_BOTON = 90;
        private const int MIN_TAMANO_BOTON = 40;

        //velocidad del boton
        int dx = VELOCIDAD_INICIAL; // Velocidad horizontal del botón
        int dy = VELOCIDAD_INICIAL; // Velocidad vertical del botón

        // Rectangulos para detectar las colisiones en las esquinas del formulario
        private Rectangle rectanguloArribaIzquierda;
        private Rectangle rectanguloArribaDerecha;
        private Rectangle rectanguloAbajoIzquierda;
        private Rectangle rectanguloAbajoDerecha;

        //==================================================
        //Objetos moviles 
        //==================================================
        // **** Los objetos moviles son los botones clonados y los formularios clonados,
        //      se almacenan en listas para poder manipularlos despues 
        private List<Button> botonesClon = new List<Button>(); // Lista para almacenar los botones clonados
        private List<Form> formulariosClon = new List<Form>(); // Lista para almacenar los formularios clonados
        private readonly Dictionary<Button, Point> velClones = new Dictionary<Button, Point>(); // Diccionario para almacenar las velocidades de los botones clonados (hecho con IA)
        private readonly Random rnd = new Random();

        // ==================================================
        // Colores de fondo para cada colision en las esquinas del formulario
        // ==================================================
        private readonly Color[] bgColores = {
            Color.FromArgb(8,  6,  28), Color.FromArgb(18, 4,  45),
            Color.FromArgb(3,  18, 42), Color.FromArgb(28, 0,  48),
            Color.FromArgb(0,  24, 38), Color.FromArgb(35, 8,  25),
        };

        // ==================================================
        // Imágenes aleatorias (Minions, pociones, nave) — cargadas desde recursos embebidos, sin rutas
        private static readonly string[] NombresRecursosImagenes = {
            "Practica3.Properties.Imagenes.minion.png",
            "Practica3.Properties.Imagenes.minionBb.png",
            "Practica3.Properties.Imagenes.minionBebe.png",
            "Practica3.Properties.Imagenes.pocion.png",
            "Practica3.Properties.Imagenes.pocionMagica.png",
        };
        private static Image CargarImagenRecurso(string nombreRecurso)
        {
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                using (var stream = asm.GetManifestResourceStream(nombreRecurso))
                {
                    if (stream != null)
                    {
                        var img = Image.FromStream(stream);
                        return new Bitmap(img);
                    }
                }
            }
            catch { /* ignorar si falta el recurso */ }
            return Resources.minion;
        }
        private static Image[] CrearArregloImagenesColision()
        {
            var lista = new List<Image> { Resources.naveEspacial };
            foreach (var nombre in NombresRecursosImagenes)
            {
                var img = CargarImagenRecurso(nombre);
                if (img != null) lista.Add(img);
            }
            return lista.ToArray();
        }
        private readonly Image[] imagenesColision = CrearArregloImagenesColision();

        private void ConfigurarVentana()
        {
            Text = "Practica 3";
            Size = new Size(WIDTH_FORMULARIO, HEIGHT_FORMULARIO);
            MaximumSize = new Size(MAX_WIDTH_FORMULARIO, MAX_HEIGHT_FORMULARIO);
            MinimumSize = new Size(420, 320);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = bgColores[0];
            DoubleBuffered = true;
            Paint += OnFormPaint;
            MouseClick += EnClickFormulario;

            Label lbl = new Label
            {
                Text = "Clic en ventana = REINICIAR   |   Clic en Boton = SALIR",
                ForeColor = Color.FromArgb(90, 140, 190),
                BackColor = Color.Transparent,
                AutoSize = true,
                Font = new Font("Consolas", 8.5f),
                Location = new Point(10, 8),
            };
            Controls.Add(lbl);
            lbl.BringToFront();
        }
        private void ConfigurarBoton()
        {
            //Config del boton
            botonPRIMARIO.Size = new Size(50, 50);
            botonPRIMARIO.Location = new Point((ClientSize.Width - botonPRIMARIO.Width) / 2, (ClientSize.Height - botonPRIMARIO.Height) / 2);
            botonPRIMARIO.Cursor = Cursors.Hand;
            botonPRIMARIO.BackgroundImageLayout = ImageLayout.Stretch;
            botonPRIMARIO.FlatStyle = FlatStyle.Flat;
            botonPRIMARIO.FlatAppearance.BorderSize = 0;
            botonPRIMARIO.Image = Resources.naveEspacial;
            botonPRIMARIO.BackgroundImage = imagenesColision[rnd.Next(imagenesColision.Length)];
            Controls.Add(botonPRIMARIO);
            botonPRIMARIO.BringToFront();
            botonPRIMARIO.Click += BotonPRIMARIO_Click; // Evento que se ejecuta al hacer clic en el botón
        }
        private void IniciarTimer()
        {
            //Config del temporizador
            temporizador.Interval = TEMPORIZADOR_INTERVAL; // Intervalo de tiempo en milisegundos
            temporizador.Tick += Temporizador_Tick; // Evento que se ejecuta en cada tick del temporizador
            temporizador.Start(); // Inicia el temporizador
        }

        // ======= FORMA PRINCIPAL ============================================================
        public Form1()
        {
            InitializeComponent();
            ConfigurarVentana();
            ConfigurarBoton();
            IniciarTimer();
        }

        //==================================================
        //Al dar clic en el boton, se cierrará el formulario
        //==================================================
        private void BotonPRIMARIO_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        //==================================================
        // Mueve el botón por el formulario, rebotando en los bordes
        //==================================================
        public void Temporizador_Tick(object sender, EventArgs e)
        {
            botonPRIMARIO.Left += dx; // Mueve el botón horizontalmente
            botonPRIMARIO.Top += dy; // Mueve el botón verticalmente

            if (botonPRIMARIO.Left < 0)
            {
                botonPRIMARIO.Left = 0;
                dx = -dx;
            }
            else if (botonPRIMARIO.Right > this.ClientSize.Width)
            {
                botonPRIMARIO.Left = this.ClientSize.Width - botonPRIMARIO.Width;
                dx = -dx;
            }

            if (botonPRIMARIO.Top < 0)
            {
                botonPRIMARIO.Top = 0;
                dy = -dy;
            }
            else if (botonPRIMARIO.Bottom > this.ClientSize.Height)
            {
                botonPRIMARIO.Top = this.ClientSize.Height - botonPRIMARIO.Height;
                dy = -dy;
            }

            // Actualiza los rectángulos de colisión en las esquinas del formulario
            ActualizarRectangulos();
            Rectangle botonRect = botonPRIMARIO.Bounds; // Obtiene el rectángulo que representa la posición y tamaño del botón
            // Colision en el borde superior izquierdo
            if (botonRect.IntersectsWith(rectanguloArribaIzquierda))
                ColisionArribaIzquierda();
            else if (botonRect.IntersectsWith(rectanguloArribaDerecha))
                ColisionArribaDerecha();
            else if (botonRect.IntersectsWith(rectanguloAbajoIzquierda))
                ColisionAbajoIzquierda();
            else if (botonRect.IntersectsWith(rectanguloAbajoDerecha))
                ColisionAbajoDerecha();

            // == COLISIONES EN LOS BORDES DEL FORMULARIO =========

            // Colision en el borde superior
            if (botonPRIMARIO.Top <= 0)
                QuitarFormulariosClonados();
            // Colision en el borde inferior
            if (botonPRIMARIO.Bottom >= this.ClientSize.Height)
                QuitarBotonesClonados();
            // Colision en el borde izquierdo
            if (botonPRIMARIO.Left <= 0)
                ClonarFormulario();
            // Colision en el borde derecho
            if (botonPRIMARIO.Right >= this.ClientSize.Width)
                ClonarBoton();


            // Mueve los botones clonados. (Hecho con IA)
            //Explicacion:
            //  El diccionario sirve para almacenar la velocidad de cada botón clonado, donde la clave es el botón y el valor
            //  es un Point que representa la velocidad en X e Y.
            //  Para cada botón clonado, se obtiene su velocidad del diccionario velClones y se actualiza su posición
            //  sumando la velocidad a las coordenadas Left y Top del botón. Luego, se verifica si el botón ha colisionado
            //  con los bordes del formulario. Si colisiona con el borde izquierdo o derecho, se ajusta su posición para que
            //  no salga del formulario y se invierte la dirección horizontal de la velocidad (v.X = -v.X). Si colisiona con
            //  el borde superior o inferior, se ajusta su posición y se invierte la dirección vertical de la velocidad (v.Y = -v.Y).
            //  Finalmente, se actualiza la velocidad en el diccionario velClones para reflejar los cambios.
            foreach (var b in botonesClon)
            {
                var v = velClones[b];

                b.Left += v.X;
                b.Top += v.Y;

                // Rebote dentro del formulario (o de la "forma" si es un Panel)
                if (b.Left < 0) //
                {
                    b.Left = 0;
                    v.X = -v.X; // Rebote en el borde izquierdo
                }
                else if (b.Right > ClientSize.Width) 
                {
                    b.Left = ClientSize.Width - b.Width;
                    v.X = -v.X;
                }

                if (b.Top < 0)
                {
                    b.Top = 0; 
                    v.Y = -v.Y;
                }
                else if (b.Bottom > ClientSize.Height)
                {
                    b.Top = ClientSize.Height - b.Height;
                    v.Y = -v.Y;
                }

                velClones[b] = v;
            }

        }

        //==================================================
        // Actualiza los rectángulos de colisión en las esquinas del formulario (hecho con IA)
        // Explicacion de cada uno:
        // rectanguloArribaIzquierda: Se posiciona en la esquina superior izquierda del formulario, con el mismo tamaño que el botón primario. Detecta colisiones en esa esquina.
        // rectanguloArribaDerecha: Se posiciona en la esquina superior derecha del formulario
        // rectanguloAbajoIzquierda: Se posiciona en la esquina inferior izquierda del formulario
        // rectanguloAbajoDerecha: Se posiciona en la esquina inferior derecha del formulario
        //==================================================
        private void ActualizarRectangulos()
        {
            rectanguloArribaIzquierda = new Rectangle(0, 0, botonPRIMARIO.Width, botonPRIMARIO.Height); 
            rectanguloArribaDerecha = new Rectangle(this.ClientSize.Width - botonPRIMARIO.Width, 0, botonPRIMARIO.Width, botonPRIMARIO.Height);
            rectanguloAbajoIzquierda = new Rectangle(0, this.ClientSize.Height - botonPRIMARIO.Height, botonPRIMARIO.Width, botonPRIMARIO.Height);
            rectanguloAbajoDerecha = new Rectangle(this.ClientSize.Width - botonPRIMARIO.Width, this.ClientSize.Height - botonPRIMARIO.Height, botonPRIMARIO.Width, botonPRIMARIO.Height);
        }


        /* ==========================================================================================================
         
          CODIGO PARA CADA COLISION DE LOS BORDE SUPERIOR, INFERIOR, IZQUIERDO Y DERECHO, SE REALIZARAN LAS SIGUIENTES ACCIONES:
          - Clonar el botón y agregarlo a la lista de botones clonados
          - Quitar los botones clonados al colisionar en el borde superior (no elimina el boton primario)
          - Clonar el formulario y agregarlo a la lista de formularios clonados
          - Quitar las formas clonadas al colisonar en el borde superior
           ==========================================================================================================
         */

        //==================================================
        // Clona el botón y lo agrega a la lista de botones clonados
        // BORDE DERECHO
        //==================================================
        public void ClonarBoton()
        {
            Button botonClonado = new Button
            {
                Location = botonPRIMARIO.Location,
                BackgroundImage = imagenesColision[rnd.Next(imagenesColision.Length)],
                BackgroundImageLayout = ImageLayout.Stretch,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Size = new Size(30, 30),
            };
            botonesClon.Add(botonClonado);
            Controls.Add(botonClonado);

            botonClonado.Left += dx; // Mueve el botón horizontalmente
            botonClonado.Top += dy; // Mueve el botón verticalmente

            int vx = rnd.Next(VELOCIDAD_MINIMA, VELOCIDAD_MAXIMA + 1); // Velocidad horizontal aleatoria para el botón clonado
            int vy = rnd.Next(VELOCIDAD_MINIMA, VELOCIDAD_MAXIMA + 1); // Velocidad vertical aleatoria para el botón clonado
            velClones[botonClonado] = new Point(vx, vy); // Almacena la velocidad del botón clonado en el diccionario velClones
        }

        //==================================================
        // Quita un boton de los clonados al colisionar en el borde inferior (no elimina el boton primario)
        // BORDE INFERIOR
        //==================================================

        public void QuitarBotonesClonados()
        {
            if (botonesClon.Count > 0)
            {
                Button botonAEliminar = botonesClon[0]; 
                Controls.Remove(botonAEliminar);
                botonesClon.RemoveAt(0);
                velClones.Remove(botonAEliminar);
                botonAEliminar.Dispose();
            }
        }

        //==================================================
        // Clona el formulario y lo agrega a la lista de formularios clonados
        // BORDE IZQUIERDO
        //==================================================
        public void ClonarFormulario()
        {
            Form formularioClonado = new Form
            {
                Size = new Size(WIDTH_FORMULARIO, HEIGHT_FORMULARIO),
                Text = "Formulario Clonado",
                StartPosition = FormStartPosition.Manual,
                Location = new Point(this.Location.X + 60, this.Location.Y + 60),
                BackColor = bgColores[rnd.Next(bgColores.Length)],
                FormBorderStyle = FormBorderStyle.FixedSingle,
                MaximizeBox = false,
                MinimizeBox = false,
            };

            formularioClonado.Paint += (s, e) => PintarEstrellas(e.Graphics, formularioClonado.ClientSize, 80); 

            // Agregar el formulario clonado a la lista de formularios clonados
            if (formulariosClon.Count < MAX_FORMULARIOS)
            {
                formulariosClon.Add(formularioClonado);
                formularioClonado.Show();
            }

        }

        //==================================================
        // Quitar las formas clonadas al colisonar
        // BORDE SUPERIOR
        //==================================================

        public void QuitarFormulariosClonados()
        {
            if (formulariosClon.Count > 0)
            {
                Form formAEliminar = formulariosClon[0];
                formAEliminar.Close();
                formulariosClon.RemoveAt(0);
            }
        }

        /* ==========================================================================================================      
          CODIGO PARA CADA COLISION DE LAS ESQUINAS DEL FORMULARIO, SE REALIZARAN LAS SIGUIENTES ACCIONES:
            - Aumentar el tamaño del botón y cambiar su imagen (arriba izquierda)
            - Aumentar el tamaño de la forma y cambiar su color de fondo (arriba derecha)
            - Disminuir el tamaño de la forma y cambiar su color de fondo (abajo izquierda)
            - Disminuir el tamaño del botón y cambiar su imagen (abajo derecha)
           ==========================================================================================================
         */

        //==================================================
        //Rectangulo Arriba izquierda 
        // AUMENTAR EL TAMAÑO DEL BOTON Y CAMBIAR SU IMAGEN random
        //==================================================
        public void ColisionArribaIzquierda()
        {
            botonPRIMARIO.Size = new Size(
                Math.Min(botonPRIMARIO.Width + 10, MAX_TAMANO_BOTON), 
                Math.Min(botonPRIMARIO.Height + 10, MAX_TAMANO_BOTON)
            );
            botonPRIMARIO.BackgroundImage = imagenesColision[rnd.Next(imagenesColision.Length)];
            botonPRIMARIO.BackgroundImageLayout = ImageLayout.Stretch;
        }

        //==================================================
        // Rectangulo Arriba derecha
        // AUMENTA EL TAMAÑO DE LA FORMA Y CAMBIA SU COLOR DE FONDO
        //==================================================
        public void ColisionArribaDerecha()
        {
            if (botonPRIMARIO.Bounds.IntersectsWith(rectanguloArribaDerecha))
            {
                this.BackColor = bgColores[rnd.Next(bgColores.Length)];
                this.Width = Math.Min(this.Width + 20, MAX_WIDTH_FORMULARIO);
                this.Height = Math.Min(this.Height + 20, MAX_HEIGHT_FORMULARIO);
            }
        }

        //==================================================
        // Rectangulo Abajo izquierda
        // DISMINUIR EL TAMAÑO DE LA FORMA Y CAMBIAR SU COLOR DE FONDO
        //==================================================
        public void ColisionAbajoIzquierda()
        {
            this.Size = new Size(
                Math.Max(this.Width - 20, 420), // Ancho mínimo para evitar que la forma desaparezca
                Math.Max(this.Height - 20, 320) // Alto mínimo para evitar que la forma desaparezca
            );
            this.BackColor = bgColores[rnd.Next(bgColores.Length)];
        }

        //=================================================
        // Rectangulo Abajo derecha
        // DISMINUIR EL TAMAÑO DEL BOTON Y CAMBIAR SU IMAGEN
        //=================================================
        public void ColisionAbajoDerecha()
        {
            botonPRIMARIO.Size = new Size(
                Math.Max(botonPRIMARIO.Width - 10, MIN_TAMANO_BOTON),
                Math.Max(botonPRIMARIO.Height - 10, MIN_TAMANO_BOTON)
            );
            botonPRIMARIO.BackgroundImage = imagenesColision[rnd.Next(imagenesColision.Length)];
        }

        //==================================================
        // Click en formulario, reiniciar
        //==================================================
        public void EnClickFormulario(object sender, EventArgs e)
        {
            foreach (Control c in this.Controls)
            {
                c.Click += EnClickFormulario;
            }
            Reiniciar();
        }

        public void Reiniciar()
        {
            // 1) Pausar movimiento
            temporizador.Stop();

            // 2) Eliminar botones clonados
            foreach (var b in botonesClon)
            {
                this.Controls.Remove(b); // Elimina el botón del formulario
                b.Dispose(); // Libera los recursos del botón clonado
            }
            botonesClon.Clear(); // Limpia la lista de botones clonados

            // 3) (Si hay formularios clonados)
            foreach (var f in formulariosClon)
            {
                if (!f.IsDisposed) f.Close();
            }
            formulariosClon.Clear();

            // 4) Restaurar botón primario (posición/tamaño/imagen)
            botonPRIMARIO.Size = new Size(50, 50);
            botonPRIMARIO.Location = new Point(
                (this.ClientSize.Width - botonPRIMARIO.Width) / 2,
                (this.ClientSize.Height - botonPRIMARIO.Height) / 2
            );
            this.Size = new Size(WIDTH_FORMULARIO, HEIGHT_FORMULARIO);
            botonPRIMARIO.BackgroundImage = imagenesColision[rnd.Next(imagenesColision.Length)];

            // 5) Restaurar velocidades
            dx = VELOCIDAD_INICIAL;
            dy = VELOCIDAD_INICIAL;

            temporizador.Start();
        }

        // DISEÑO DE LA INTERFAZ  hecho con IA
        private void OnFormPaint(object sender, PaintEventArgs e)
        {
            PintarEstrellas(e.Graphics, ClientSize, 120);
        }

        //Hecho con IA
        private void PintarEstrellas(Graphics g, Size sz, int cantidad)
        {
            Random r = new Random(sz.Width * 13 + sz.Height * 7); // Semilla basada en el tamaño del formulario para que las estrellas sean consistentes en cada formulario
            Color[] cols = { // Colores de las estrellas con diferentes niveles de transparencia
                Color.FromArgb(200, 255, 255, 255), Color.FromArgb(140, 255, 240, 180),
                Color.FromArgb(120, 150, 200, 255), Color.FromArgb(100, 255, 150, 200),
            };
            for (int i = 0; i < cantidad; i++) // Dibuja 'cantidad' estrellas
            {
                int ss = r.Next(1, 4); // Tamaño aleatorio para cada estrella (1 a 3 píxeles)
                using (SolidBrush br = new SolidBrush(cols[r.Next(cols.Length)])) // Elige un color aleatorio para la estrella
                    g.FillEllipse(br, r.Next(0, sz.Width), r.Next(0, sz.Height), ss, ss); // Dibuja la estrella como un pequeño círculo en una posición aleatoria dentro del formulario
            }

        }
    }
}
