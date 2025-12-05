import { initializeApp } from 'firebase/app';
import { getAuth } from 'firebase/auth';

const firebaseConfig = {
  apiKey: "AIzaSyAeyZ11lRJDmV7cB41YJ-gdtj2tkvefWk0",
  authDomain: "airbnb-clone-b24a1.firebaseapp.com",
  projectId: "airbnb-clone-b24a1",
  storageBucket: "airbnb-clone-b24a1.appspot.com",
  messagingSenderId: "658981284978",
  appId: "1:658981284978:web:1304f9e796c2610ead26eb",
  measurementId: "G-E4VYTC8XVV"
};

// Initialize Firebase
const app = initializeApp(firebaseConfig);

// Export auth instance
export const firebaseAuth = getAuth(app);
