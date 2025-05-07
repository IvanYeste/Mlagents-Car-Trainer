# ML-Agents Car Trainer 🚗🤖

Este proyecto es un simulador desarrollado en **Unity + ML-Agents** cuyo objetivo es entrenar a un agente (un coche) para que sea capaz de recorrer un circuito de forma óptima, minimizando el tiempo y completando todos los checkpoints.

## 🎮 Descripción

- Utiliza **ML-Agents Toolkit** para entrenar mediante aprendizaje por refuerzo.
- El agente es un coche que aprende a recorrer un circuito cerrado.
- El objetivo del agente es completar la vuelta en el menor tiempo posible.
- Se han implementado **checkpoints** en el circuito para guiar el aprendizaje del agente.
- El coche es penalizado por salirse del circuito o por no avanzar correctamente.

## 🧠 Tecnologías y herramientas utilizadas

- **Unity** (versión recomendada: indicar versión exacta que usaste → ejemplo: Unity 2022.3.10f1)
- **ML-Agents Toolkit**
- **TensorFlow (opcional, dependiendo del backend de ML-Agents)**
- **Python (para lanzar entrenamientos)**

## 🎬 Demostración

¡Aquí puedes ver el comportamiento del coche tras el entrenamiento!

<img src="./CocheFinalGIF.gif" width="600">



## 🚦 Entrenamiento

Los modelos se entrenaron utilizando la herramienta `mlagents-learn` con configuración personalizada de hiperparámetros para lograr un buen equilibrio entre exploración y explotación.

Ejemplo de comando:

```bash
mlagents-learn config/trainer_config.yaml --run-id=CircuitTrainer --train

## 📈 Resultados

Tras el entrenamiento, el agente ha conseguido:

- ✅ Completar vueltas al circuito sin salirse.
- ✅ Seguir la trazada ideal gracias a los checkpoints.
- ✅ Optimizar su velocidad para minimizar los tiempos por vuelta.
- ✅ Evitar errores frecuentes como quedarse atascado o desviarse.

Estos resultados muestran que el modelo ha aprendido una estrategia eficaz para recorrer el circuito de manera estable y eficiente.

---

## 🚧 Mejoras futuras

Algunas ideas para seguir ampliando el proyecto son:

- 🔧 **Añadir diferentes tipos de circuitos** para fomentar la generalización del agente.
- 🚘 **Implementar rivales controlados por IA** para crear escenarios de competición.
- 🧠 **Desarrollar un sistema de recompensas más complejo**, teniendo en cuenta criterios como velocidad óptima o trazada perfecta.
- 📊 **Visualizar las estadísticas de rendimiento** directamente en la interfaz de Unity.

---

## 📜 Licencia

Este proyecto se distribuye bajo la **Licencia MIT**.  
Eres libre de usar, modificar y distribuir el código, siempre que mantengas los créditos al autor original.

---

## ✨ Autor

Proyecto desarrollado por [Ivan Yeste](https://github.com/IvanYeste).  
Si te interesa colaborar, sugerir mejoras o tienes cualquier duda, ¡no dudes en contactar o abrir un issue en el repositorio!

---
