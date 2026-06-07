from sqlalchemy import Column, Integer, String, Numeric, create_engine
from sqlalchemy.orm import DeclarativeBase, Session


class Base(DeclarativeBase):
    pass


class Cpu(Base):
    __tablename__ = "cpu"

    Id = Column(Integer, primary_key=True, autoincrement=True)
    Name = Column(String(200), nullable=False)
    Manufacturer = Column(String(100), nullable=False)
    Price = Column(Numeric(18, 0), nullable=False)
    Socket = Column(String(50), nullable=False)
    CoreCount = Column(Integer, nullable=False, default=0)
    ThreadCount = Column(Integer, nullable=False, default=0)
    BaseClock = Column(Numeric(5, 2), nullable=False, default=0)
    BoostClock = Column(Numeric(5, 2), nullable=False, default=0)
    TDP = Column(Integer, nullable=False, default=0)
    ApproximatePerformance = Column(Numeric(10, 2), nullable=False, default=0)
    ImageUrl = Column(String, nullable=True)
    Stock = Column(Integer, nullable=False, default=0)


class Motherboard(Base):
    __tablename__ = "motherboard"

    Id = Column(Integer, primary_key=True, autoincrement=True)
    Name = Column(String(200), nullable=False)
    Manufacturer = Column(String(100), nullable=False)
    Price = Column(Numeric(18, 0), nullable=False)
    SocketCompatibility = Column(String(50), nullable=False)
    FormFactor = Column(String(20), nullable=False)
    MemoryCompatibility = Column(String(10), nullable=False)
    MemorySlots = Column(Integer, nullable=False, default=4)
    MaxMemoryCapacity = Column(Integer, nullable=False, default=128)
    Chipset = Column(String(20), nullable=False, default="")
    ImageUrl = Column(String, nullable=True)
    Stock = Column(Integer, nullable=False, default=0)


class Memory(Base):
    __tablename__ = "memory"

    Id = Column(Integer, primary_key=True, autoincrement=True)
    Name = Column(String(200), nullable=False)
    Manufacturer = Column(String(100), nullable=False)
    Price = Column(Numeric(18, 0), nullable=False)
    Type = Column(String(10), nullable=False)
    Capacity = Column(Integer, nullable=False, default=0)
    Modules = Column(Integer, nullable=False, default=1)
    Speed = Column(Integer, nullable=False, default=0)
    Profile = Column(String(30), nullable=False, default="")
    ImageUrl = Column(String, nullable=True)
    Stock = Column(Integer, nullable=False, default=0)


class VideoCard(Base):
    __tablename__ = "video_card"

    Id = Column(Integer, primary_key=True, autoincrement=True)
    Name = Column(String(200), nullable=False)
    Manufacturer = Column(String(100), nullable=False)
    Price = Column(Numeric(18, 0), nullable=False)
    VRAM = Column(Integer, nullable=False, default=0)
    Length = Column(Integer, nullable=False, default=0)
    TDP = Column(Integer, nullable=False, default=0)
    ApproximatePerformance = Column(Numeric(10, 2), nullable=False, default=0)
    ImageUrl = Column(String, nullable=True)
    Stock = Column(Integer, nullable=False, default=0)


class PowerSupply(Base):
    __tablename__ = "power_supply"

    Id = Column(Integer, primary_key=True, autoincrement=True)
    Name = Column(String(200), nullable=False)
    Manufacturer = Column(String(100), nullable=False)
    Price = Column(Numeric(18, 0), nullable=False)
    Wattage = Column(Integer, nullable=False, default=0)
    Efficiency = Column(String(30), nullable=False, default="")
    Modular = Column(String(20), nullable=False, default="")
    PsuFormFactor = Column(String(10), nullable=False, default="ATX")
    ImageUrl = Column(String, nullable=True)
    Stock = Column(Integer, nullable=False, default=0)


class CaseEnclosure(Base):
    __tablename__ = "case_enclosure"

    Id = Column(Integer, primary_key=True, autoincrement=True)
    Name = Column(String(200), nullable=False)
    Manufacturer = Column(String(100), nullable=False)
    Price = Column(Numeric(18, 0), nullable=False)
    FormFactorSupport = Column(String(50), nullable=False)
    MaxVGALength = Column(Integer, nullable=False, default=0)
    Color = Column(String(30), nullable=True)
    CaseType = Column(String(20), nullable=False, default="")
    RadiatorSupport = Column(String(50), nullable=False, default="")
    ImageUrl = Column(String, nullable=True)
    Stock = Column(Integer, nullable=False, default=0)


class Storage(Base):
    __tablename__ = "storage"

    Id = Column(Integer, primary_key=True, autoincrement=True)
    Name = Column(String(200), nullable=False)
    Manufacturer = Column(String(100), nullable=False)
    Price = Column(Numeric(18, 0), nullable=False)
    Type = Column(String(20), nullable=False)
    Capacity = Column(Integer, nullable=False, default=0)
    Interface = Column(String(20), nullable=False, default="")
    ReadSpeed = Column(Integer, nullable=False, default=0)
    WriteSpeed = Column(Integer, nullable=False, default=0)
    ImageUrl = Column(String, nullable=True)
    Stock = Column(Integer, nullable=False, default=0)


class CpuCooler(Base):
    __tablename__ = "cpu_cooler"

    Id = Column(Integer, primary_key=True, autoincrement=True)
    Name = Column(String(200), nullable=False)
    Manufacturer = Column(String(100), nullable=False)
    Price = Column(Numeric(18, 0), nullable=False)
    SocketCompatibility = Column(String(200), nullable=False)
    MaxTDP = Column(Integer, nullable=False, default=0)
    Height = Column(Integer, nullable=False, default=0)
    Type = Column(String(30), nullable=False, default="")
    ImageUrl = Column(String, nullable=True)
    Stock = Column(Integer, nullable=False, default=0)
