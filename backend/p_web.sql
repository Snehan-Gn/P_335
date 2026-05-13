-- phpMyAdmin SQL Dump
-- version 5.2.3
-- https://www.phpmyadmin.net/
--
-- Hôte : db
-- Généré le : mar. 12 mai 2026 à 14:00
-- Version du serveur : 8.0.45
-- Version de PHP : 8.3.30

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Base de données : `p_web`
--
CREATE DATABASE IF NOT EXISTS `p_web` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;
USE `p_web`;

-- --------------------------------------------------------

--
-- Structure de la table `contains`
--

CREATE TABLE IF NOT EXISTS `contains` (
  `t_book_fk` int NOT NULL,
  `t_category_fk` int NOT NULL,
  PRIMARY KEY (`t_book_fk`,`t_category_fk`),
  KEY `t_category_fk` (`t_category_fk`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Structure de la table `possess`
--

CREATE TABLE IF NOT EXISTS `possess` (
  `t_book_fk` int NOT NULL,
  `t_user_fk` int NOT NULL,
  PRIMARY KEY (`t_book_fk`,`t_user_fk`),
  KEY `t_user_fk` (`t_user_fk`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Structure de la table `t_book`
--

CREATE TABLE IF NOT EXISTS `t_book` (
  `book_id` int NOT NULL AUTO_INCREMENT,
  `title` text CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `author` text CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `description` text CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `publish_date` date DEFAULT NULL,
  `language_` text CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `isbn` text,
  `url` text CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `cover_image_url` text CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  PRIMARY KEY (`book_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Structure de la table `t_category`
--

CREATE TABLE IF NOT EXISTS `t_category` (
  `category_id` int NOT NULL AUTO_INCREMENT,
  `name` text CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  PRIMARY KEY (`category_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Structure de la table `t_comment`
--

CREATE TABLE IF NOT EXISTS `t_comment` (
  `comment_id` int NOT NULL AUTO_INCREMENT,
  `title` text CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `message` text CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `rating` int NOT NULL,
  `t_book_fk` int DEFAULT NULL,
  `t_user_fk` int NOT NULL,
  PRIMARY KEY (`comment_id`),
  KEY `t_book_fk` (`t_book_fk`),
  KEY `t_user_fk` (`t_user_fk`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Structure de la table `t_user`
--

CREATE TABLE IF NOT EXISTS `t_user` (
  `user_id` int NOT NULL AUTO_INCREMENT,
  `username` varchar(255) NOT NULL,
  `email` varchar(255) NOT NULL,
  `password_hash` varchar(255) NOT NULL,
  PRIMARY KEY (`user_id`),
  UNIQUE KEY `username` (`username`),
  UNIQUE KEY `email` (`email`),
  UNIQUE KEY `password_hash` (`password_hash`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

--
-- Contraintes pour les tables déchargées
--

--
-- Contraintes pour la table `contains`
--
ALTER TABLE `contains`
  ADD CONSTRAINT `contains_ibfk_1` FOREIGN KEY (`t_book_fk`) REFERENCES `t_book` (`book_id`),
  ADD CONSTRAINT `contains_ibfk_2` FOREIGN KEY (`t_category_fk`) REFERENCES `t_category` (`category_id`);

--
-- Contraintes pour la table `possess`
--
ALTER TABLE `possess`
  ADD CONSTRAINT `possess_ibfk_1` FOREIGN KEY (`t_book_fk`) REFERENCES `t_book` (`book_id`),
  ADD CONSTRAINT `possess_ibfk_2` FOREIGN KEY (`t_user_fk`) REFERENCES `t_user` (`user_id`);

--
-- Contraintes pour la table `t_comment`
--
ALTER TABLE `t_comment`
  ADD CONSTRAINT `t_comment_ibfk_1` FOREIGN KEY (`t_book_fk`) REFERENCES `t_book` (`book_id`),
  ADD CONSTRAINT `t_comment_ibfk_2` FOREIGN KEY (`t_user_fk`) REFERENCES `t_user` (`user_id`);
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
