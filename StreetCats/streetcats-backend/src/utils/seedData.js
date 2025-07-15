const mongoose = require('mongoose');
const User = require('../models/User');
const Cat = require('../models/Cat');
const Comment = require('../models/Comment');
const connectDB = require('../config/database');

const users = [
  {
    username: 'marco_napoli',
    email: 'marco@example.com',
    fullName: 'Marco Esposito',
    password: 'password123',
    role: 'user'
  },
  {
    username: 'giulia_cats',
    email: 'giulia@example.com',
    fullName: 'Giulia Romano',
    password: 'password123',
    role: 'user'
  },
  {
    username: 'toni_gatti',
    email: 'antonio@example.com',
    fullName: 'Antonio Bianchi',
    password: 'password123',
    role: 'user'
  },
  {
    username: 'admin',
    email: 'admin@streetcats.com',
    fullName: 'Admin STREETCATS',
    password: 'admin123',
    role: 'admin'
  }
];

const cats = [
  {
    name: 'Micio',
    description: 'Gatto arancione molto socievole, si fa accarezzare facilmente',
    color: 'Arancione',
    status: 'sano',
    location: {
      coordinates: [14.2522, 40.8467],
      address: 'Via Toledo, 145',
      city: 'Napoli',
      postalCode: '80134'
    }
  },
  {
    name: 'Luna',
    description: 'Gatta bianca molto timida, sterilizzata',
    color: 'Bianco',
    status: 'sterilizzato',
    location: {
      coordinates: [14.2556, 40.8500],
      address: 'Piazza Dante, 1',
      city: 'Napoli',
      postalCode: '80135'
    }
  },
  {
    name: 'Romeo',
    description: 'Gatto nero con ferita alla zampa, ha bisogno di cure',
    color: 'Nero',
    status: 'ferito',
    location: {
      coordinates: [14.2575, 40.8489],
      address: 'Spaccanapoli, 45',
      city: 'Napoli',
      postalCode: '80138'
    }
  },
  {
    name: 'Stella',
    description: 'Gatta tigrata grigia, molto affettuosa',
    color: 'Grigio tigrato',
    status: 'sano',
    location: {
      coordinates: [14.2450, 40.8356],
      address: 'Via Chiaia, 89',
      city: 'Napoli',
      postalCode: '80121'
    }
  },
  {
    name: 'Napoleone',
    description: 'Gatto grande color zenzero, molto territoriale',
    color: 'Rosso',
    status: 'sano',
    location: {
      coordinates: [14.2472, 40.8278],
      address: 'Castel dell\'Ovo, 1',
      city: 'Napoli',
      postalCode: '80132'
    }
  },
  {
    name: 'Bianca',
    description: 'Gatta persiana bianca, adottata recentemente',
    color: 'Bianco',
    status: 'adottato',
    location: {
      coordinates: [14.2344, 40.8589],
      address: 'Vomero, Via Kerbaker 23',
      city: 'Napoli',
      postalCode: '80129'
    }
  }
];

const comments = [
  {
    text: 'Che bel gattino! Sembra molto sano e felice.',
    userName: 'marco_napoli'
  },
  {
    text: 'L\'ho visto ieri sera vicino al bar. Aveva fame.',
    userName: 'giulia_cats'
  },
  {
    text: 'Questo micio è molto socievole, si fa accarezzare.',
    userName: 'toni_gatti'
  },
  {
    text: 'Beautiful cat! Very friendly with people.',
    userName: 'marco_napoli'
  },
  {
    text: 'Attenzione, ha una ferita alla zampa sinistra.',
    userName: 'giulia_cats'
  },
  {
    text: 'È sterilizzato, ho visto il taglio all\'orecchio.',
    userName: 'toni_gatti'
  }
];

const seedDatabase = async () => {
  try {
    await connectDB();
    
    // Pulisci database esistente
    await User.deleteMany({});
    await Cat.deleteMany({});
    await Comment.deleteMany({});
    
    console.log('Database pulito');
    
    // Crea utenti
    const createdUsers = await User.create(users);
    console.log(`${createdUsers.length} utenti creati`);
    
    // Crea gatti
    const catsWithUsers = cats.map((cat, index) => ({
      ...cat,
      createdBy: createdUsers[index % createdUsers.length]._id
    }));
    
    const createdCats = await Cat.create(catsWithUsers);
    console.log(`${createdCats.length} gatti creati`);
    
    // Crea commenti
    const commentsWithData = comments.map((comment, index) => ({
      ...comment,
      catId: createdCats[index % createdCats.length]._id,
      userId: createdUsers.find(u => u.username === comment.userName)._id
    }));
    
    const createdComments = await Comment.create(commentsWithData);
    console.log(`${createdComments.length} commenti creati`);
    
    console.log('✅ Seed data inseriti con successo!');
    process.exit(0);
  } catch (error) {
    console.error('❌ Errore durante il seed:', error);
    process.exit(1);
  }
};

// Esegui seed se chiamato direttamente
if (require.main === module) {
  seedDatabase();
}

module.exports = seedDatabase;